using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ParkingSubscription.Application.Abstractions;
using ParkingSubscription.Domain.Entities;
using ParkingSubscription.Infrastructure.Persistence;

namespace ParkingSubscription.Infrastructure.Payments;

/// <summary>
/// Checkbox Online (api.checkbox.ua) cloud fiscalization behind <see cref="IFiscalProvider"/>.
/// Ported from the tested UAeReceipt CheckBoxOnlineProvider, adapted to this app:
///   - PIN sign-in (409 → signout + retry) → Bearer token
///   - auto-open shift (409 = already open, OK)
///   - sell one good (the plan) paid CASHLESS, poll the receipt until DONE
///   - receipt shown to the user as the rendered PNG (/receipts/{id}/png)
///
/// Credentials come from the single-row <see cref="FiscalGatewayConfig"/> (PIN + license
/// decrypted via <see cref="ICredentialProtector"/>). Endpoints use a LEADING slash; the
/// base URL has no trailing slash.
/// </summary>
public sealed class CheckboxOnlineFiscalProvider(
    IHttpClientFactory httpClientFactory,
    AppDbContext db,
    ICredentialProtector protector,
    ILogger<CheckboxOnlineFiscalProvider> logger) : IFiscalProvider
{
    public const string HttpClientName = "Checkbox";
    private const string ClientName = "SilverBreeze";
    private const string ClientVersion = "1.0.0";

    public async Task<FiscalReceipt> FiscalizeAsync(Payment payment, CancellationToken ct = default)
    {
        var cfg = await LoadAsync(ct);
        var token = await AuthorizeAsync(cfg, ct);
        await EnsureShiftAsync(cfg, token, ct);

        var plan = await db.SubscriptionPlans.AsNoTracking()
            .FirstAsync(p => p.Id == payment.SubscriptionPlanId, ct);

        // One good = the plan; price and payment value in kopiykas (== AmountMinor).
        var good = new Dictionary<string, object?>
        {
            ["code"] = plan.Code,
            ["name"] = plan.Name,
            ["price"] = payment.AmountMinor,
        };
        if (cfg.TaxCode is int tax) good["tax"] = new[] { tax };

        var sell = new
        {
            id = Guid.NewGuid().ToString(),
            goods = new[]
            {
                new { good, good_id = Guid.NewGuid().ToString(), quantity = 1000 }
            },
            payments = new[]
            {
                new { type = "CASHLESS", value = payment.AmountMinor, label = "Картка" }
            },
        };

        var sellResp = await SendAsync(HttpMethod.Post, cfg.BaseUrl, "/api/v1/receipts/sell", cfg, token, sell, ct);
        using var sellDoc = JsonDocument.Parse(sellResp);
        var receiptId = sellDoc.RootElement.GetProperty("id").GetString()
            ?? throw new InvalidOperationException("Checkbox: sell returned no receipt id.");

        var taxUrl = await PollUntilDoneAsync(cfg, token, receiptId, ct);
        logger.LogInformation("[Checkbox] Fiscalized payment {PaymentId} → receipt {ReceiptId}", payment.Id, receiptId);
        return new FiscalReceipt(receiptId, taxUrl ?? string.Empty);
    }

    public async Task<FiscalReceiptImage?> GetReceiptImageAsync(string receiptId, CancellationToken ct = default)
    {
        var cfg = await LoadAsync(ct);
        var token = await AuthorizeAsync(cfg, ct);

        using var http = httpClientFactory.CreateClient(HttpClientName);
        using var req = new HttpRequestMessage(HttpMethod.Get, $"{cfg.BaseUrl}/api/v1/receipts/{receiptId}/png");
        ApplyHeaders(req, cfg, token);
        req.Headers.Accept.Clear();
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("image/png"));

        var resp = await http.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode)
        {
            logger.LogWarning("[Checkbox] Receipt image {ReceiptId}: HTTP {Status}", receiptId, (int)resp.StatusCode);
            return null;
        }
        var bytes = await resp.Content.ReadAsByteArrayAsync(ct);
        var contentType = resp.Content.Headers.ContentType?.MediaType ?? "image/png";
        return new FiscalReceiptImage(bytes, contentType);
    }

    // ── Flow helpers ────────────────────────────────────────────────────────

    private async Task<string> AuthorizeAsync(CheckboxCredentials cfg, CancellationToken ct)
    {
        using var http = httpClientFactory.CreateClient(HttpClientName);

        async Task<HttpResponseMessage> SignInAsync()
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, $"{cfg.BaseUrl}/api/v1/cashier/signinPinCode")
            {
                Content = JsonBody(new { pin_code = cfg.PinCode }),
            };
            ApplyHeaders(req, cfg, token: null);
            return await http.SendAsync(req, ct);
        }

        var resp = await SignInAsync();
        if ((int)resp.StatusCode == 409)
        {
            // Session already active — sign out and retry once.
            using (var signout = new HttpRequestMessage(HttpMethod.Post, $"{cfg.BaseUrl}/api/v1/cashier/signout"))
            {
                ApplyHeaders(signout, cfg, token: null);
                await http.SendAsync(signout, ct);
            }
            resp.Dispose();
            resp = await SignInAsync();
        }

        var body = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"Checkbox sign-in failed: HTTP {(int)resp.StatusCode} {body}");

        using var doc = JsonDocument.Parse(body);
        return doc.RootElement.GetProperty("access_token").GetString()
            ?? throw new InvalidOperationException("Checkbox sign-in returned no access_token.");
    }

    private async Task EnsureShiftAsync(CheckboxCredentials cfg, string token, CancellationToken ct)
    {
        using var http = httpClientFactory.CreateClient(HttpClientName);

        // Request opening (idempotent: 409 = already opening/open).
        using (var openReq = new HttpRequestMessage(HttpMethod.Post, $"{cfg.BaseUrl}/api/v1/shifts"))
        {
            ApplyHeaders(openReq, cfg, token);
            var openResp = await http.SendAsync(openReq, ct);
            if (!openResp.IsSuccessStatusCode && (int)openResp.StatusCode != 409)
            {
                var body = await openResp.Content.ReadAsStringAsync(ct);
                throw new InvalidOperationException($"Checkbox open-shift failed: HTTP {(int)openResp.StatusCode} {body}");
            }
        }

        // Opening a shift is ASYNC — poll the current shift until it is OPENED before selling,
        // otherwise /receipts/sell returns 400 shift.not_opened.
        const int maxAttempts = 20;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, $"{cfg.BaseUrl}/api/v1/cashier/shift");
            ApplyHeaders(req, cfg, token);
            var resp = await http.SendAsync(req, ct);
            if (resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync(ct);
                if (!string.IsNullOrWhiteSpace(body))
                {
                    using var doc = JsonDocument.Parse(body);
                    var status = doc.RootElement.TryGetProperty("status", out var s) ? s.GetString() : null;
                    if (status == "OPENED") return;
                    if (status == "CLOSED")
                        throw new InvalidOperationException("Checkbox shift is CLOSED after an open attempt.");
                }
            }
            await Task.Delay(500, ct);
        }
        throw new InvalidOperationException($"Checkbox shift did not reach OPENED after {maxAttempts} attempts.");
    }

    private async Task<string?> PollUntilDoneAsync(CheckboxCredentials cfg, string token, string receiptId, CancellationToken ct)
    {
        const int maxAttempts = 20;
        using var http = httpClientFactory.CreateClient(HttpClientName);
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, $"{cfg.BaseUrl}/api/v1/receipts/{receiptId}");
            ApplyHeaders(req, cfg, token);
            var resp = await http.SendAsync(req, ct);
            var body = await resp.Content.ReadAsStringAsync(ct);
            if (resp.IsSuccessStatusCode)
            {
                using var doc = JsonDocument.Parse(body);
                var status = doc.RootElement.TryGetProperty("status", out var s) ? s.GetString() : null;
                if (status == "DONE")
                    return doc.RootElement.TryGetProperty("tax_url", out var u) ? u.GetString() : null;
                if (status == "ERROR")
                    throw new InvalidOperationException($"Checkbox receipt {receiptId} finished with ERROR: {body}");
            }
            await Task.Delay(500, ct);
        }
        throw new InvalidOperationException($"Checkbox receipt {receiptId} not DONE after {maxAttempts} attempts.");
    }

    // ── HTTP / config plumbing ───────────────────────────────────────────────

    private async Task<string> SendAsync(
        HttpMethod method, string baseUrl, string path, CheckboxCredentials cfg, string token, object body, CancellationToken ct)
    {
        using var http = httpClientFactory.CreateClient(HttpClientName);
        using var req = new HttpRequestMessage(method, $"{baseUrl}{path}") { Content = JsonBody(body) };
        ApplyHeaders(req, cfg, token);
        var resp = await http.SendAsync(req, ct);
        var respBody = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
            throw new InvalidOperationException($"Checkbox {path} failed: HTTP {(int)resp.StatusCode} {respBody}");
        return respBody;
    }

    private void ApplyHeaders(HttpRequestMessage req, CheckboxCredentials cfg, string? token)
    {
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        req.Headers.Add("X-Client-Name", ClientName);
        req.Headers.Add("X-Client-Version", ClientVersion);
        if (!string.IsNullOrWhiteSpace(cfg.LicenseKey))
            req.Headers.Add("X-License-Key", cfg.LicenseKey);
        if (!string.IsNullOrWhiteSpace(token))
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private static StringContent JsonBody(object body) =>
        new(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

    private async Task<CheckboxCredentials> LoadAsync(CancellationToken ct)
    {
        var row = await db.FiscalGatewayConfigs.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == FiscalGatewayConfig.SingletonId, ct);

        string Decrypt(string? enc)
        {
            if (string.IsNullOrEmpty(enc)) return string.Empty;
            try { return protector.Unprotect(enc); }
            catch (Exception ex) { logger.LogWarning(ex, "[Checkbox] Failed to decrypt a credential"); return string.Empty; }
        }

        var cfg = new CheckboxCredentials(
            PinCode: Decrypt(row?.PinCodeEncrypted),
            LicenseKey: Decrypt(row?.LicenseKeyEncrypted),
            BaseUrl: (row?.BaseUrl ?? "https://api.checkbox.ua").TrimEnd('/'),
            TaxCode: row?.TaxCode);

        if (string.IsNullOrEmpty(cfg.PinCode))
            logger.LogError("[Checkbox] Credentials not configured (PIN missing). Seed via Fiscal__Checkbox__* env vars.");
        return cfg;
    }

    private sealed record CheckboxCredentials(string PinCode, string LicenseKey, string BaseUrl, int? TaxCode);
}
