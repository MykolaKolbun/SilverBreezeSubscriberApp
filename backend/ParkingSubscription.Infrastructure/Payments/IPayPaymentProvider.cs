using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ParkingSubscription.Application.Abstractions;
using ParkingSubscription.Infrastructure.Persistence;

namespace ParkingSubscription.Infrastructure.Payments;

/// <summary>
/// Live iPay.ua payment provider — hosted-page PaymentCreate (XML /api302).
/// Ported from the EVCharging IPayPaymentProvider, adapted to this app's single-merchant
/// B2C model: credentials are read from the singleton <c>PaymentGatewayConfig</c> row and
/// the SignKey is decrypted via <see cref="ICredentialProtector"/>.
///
/// Auth (two-stage, mirrors iPay's PHP sha1(microtime)+hash_hmac):
///   Salt = SHA-1( unix_timestamp.4decimal )  → lowercase hex
///   Sign = HMAC-SHA-512( salt, SignKey )      → lowercase hex
///
/// SignKey is NEVER logged in plaintext.
/// </summary>
public sealed class IPayPaymentProvider(
    IHttpClientFactory httpClientFactory,
    AppDbContext db,
    ICredentialProtector protector,
    ILogger<IPayPaymentProvider> logger) : IPaymentProvider
{
    public const string HttpClientName = "iPay";

    private const int LifetimeHours = 24;
    private const string Lang = "uk";
    private const string SandboxBaseUrl = "https://sandbox-checkout.ipay.ua/api302";

    private IPayCredentials? _cached;

    // ── IPaymentProvider ─────────────────────────────────────────────────────

    public async Task<PaymentIntent> CreatePaymentAsync(PaymentInitiation initiation, CancellationToken ct = default)
    {
        var cfg = await LoadAsync(ct);
        var (salt, sign) = GenerateAuth(cfg.SignKey);

        var xml = BuildPaymentXml(salt, sign, cfg.MerchantId, initiation);
        var responseXml = await PostAsync(SerializeXml(xml), cfg.BaseUrl, ct);
        var (pid, url) = ParseCreateResponse(responseXml);

        if (string.IsNullOrEmpty(pid) || string.IsNullOrEmpty(url))
        {
            logger.LogError("[iPay] Missing pid/url in response. Raw XML: {Xml}", responseXml);
            throw new InvalidOperationException($"iPay did not return a payment URL. Response: {responseXml}");
        }

        logger.LogInformation("[iPay] Payment initiated pid={Pid}", pid);
        return new PaymentIntent(pid, url);
    }

    public async Task<ProviderPaymentStatusResult> GetStatusAsync(string providerPaymentId, CancellationToken ct = default)
    {
        var cfg = await LoadAsync(ct);
        var responseXml = await PostPidRequestAsync(providerPaymentId, "status", cfg, ct);
        var root = TryParseRoot(responseXml);
        if (root is null)
        {
            logger.LogWarning("[iPay] GetStatus pid={Pid}: unparseable response", providerPaymentId);
            return new ProviderPaymentStatusResult(ProviderPaymentStatus.Unknown, 0);
        }

        var statusRaw = root.Element("status")?.Value;
        // iPay status response: <invoice> = amount without commission (net, kopiykas).
        var amountStr = root.Element("invoice")?.Value;
        var amountMinor = long.TryParse(amountStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var a) ? a : 0L;
        var code = MapStatusCode(statusRaw);

        logger.LogInformation("[iPay] GetStatus pid={Pid} status={Code}({Raw}) amountMinor={Amount}",
            providerPaymentId, code, statusRaw, amountMinor);

        return new ProviderPaymentStatusResult(code, amountMinor);
    }

    /// <summary>
    /// PaymentReversal uses iPay's separate JSON API (login+time+sign) — not implemented.
    /// A refund is a no-op here so the app's refund path does not fault; wire the real
    /// reversal endpoint when its docs/credentials are available.
    /// </summary>
    public Task RefundAsync(string providerPaymentId, CancellationToken ct = default)
    {
        logger.LogWarning("[iPay] RefundAsync not implemented — reversal skipped for pid={Pid}", providerPaymentId);
        return Task.CompletedTask;
    }

    // ── Credentials ───────────────────────────────────────────────────────────

    private async Task<IPayCredentials> LoadAsync(CancellationToken ct)
    {
        if (_cached is not null) return _cached;

        var row = await db.PaymentGatewayConfigs.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == Domain.Entities.PaymentGatewayConfig.SingletonId, ct);

        var signKey = string.Empty;
        if (!string.IsNullOrEmpty(row?.SignKeyEncrypted))
        {
            try { signKey = protector.Unprotect(row.SignKeyEncrypted); }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "[iPay] Failed to decrypt SignKey — using empty key");
            }
        }

        _cached = new IPayCredentials(
            MerchantId: row?.MerchantId ?? string.Empty,
            SignKey: signKey,
            BaseUrl: string.IsNullOrWhiteSpace(row?.BaseUrl) ? SandboxBaseUrl : row!.BaseUrl!);

        if (string.IsNullOrEmpty(_cached.MerchantId) || string.IsNullOrEmpty(_cached.SignKey))
            logger.LogError("[iPay] Credentials not configured (MerchantId/SignKey missing). " +
                            "Seed the PaymentGatewayConfig row via Payment__iPay__* env vars.");

        return _cached;
    }

    private sealed record IPayCredentials(string MerchantId, string SignKey, string BaseUrl);

    // ── XML building ──────────────────────────────────────────────────────────

    private static XElement BuildPaymentXml(string salt, string sign, string merchantId, PaymentInitiation req)
    {
        // transaction.info carries our order binding as JSON; iPay echoes it back unchanged.
        var info = JsonSerializer.Serialize(new
        {
            orderId = req.Reference,
            view_params = new { cancel_button = 1, retry_button = 1 },
        });

        return new XElement("payment",
            new XElement("auth",
                new XElement("mch_id", merchantId),
                new XElement("salt", salt),
                new XElement("sign", sign)),
            new XElement("urls",
                new XElement("good", req.SuccessUrl),
                new XElement("bad", req.FailureUrl),
                new XElement("auto_redirect_good", 1),
                new XElement("auto_redirect_bad", 1)),
            new XElement("transactions",
                new XElement("transaction",
                    new XElement("amount", req.AmountMinor),   // iPay expects kopiykas (== our AmountMinor)
                    new XElement("currency", req.Currency),
                    new XElement("desc", req.Description),
                    new XElement("info", info))),
            new XElement("lifetime", LifetimeHours),
            new XElement("lang", Lang));
    }

    private async Task<string> PostPidRequestAsync(string pid, string action, IPayCredentials cfg, CancellationToken ct)
    {
        var (salt, sign) = GenerateAuth(cfg.SignKey);
        var xml = new XElement("payment",
            new XElement("auth",
                new XElement("mch_id", cfg.MerchantId),
                new XElement("salt", salt),
                new XElement("sign", sign)),
            new XElement("action", action),
            new XElement("pid", pid));
        return await PostAsync(SerializeXml(xml), cfg.BaseUrl, ct);
    }

    /// <summary>
    /// Serialises to a UTF-8 XML string WITHOUT a BOM — a BOM in the form-encoded "data"
    /// field would make iPay reject the request.
    /// </summary>
    private static string SerializeXml(XElement root)
    {
        var noBomUtf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        var settings = new XmlWriterSettings { Encoding = noBomUtf8, Indent = true };
        using var ms = new MemoryStream();
        using (var xw = XmlWriter.Create(ms, settings))
            new XDocument(new XDeclaration("1.0", "UTF-8", null), root).Save(xw);
        return noBomUtf8.GetString(ms.ToArray());
    }

    // ── Signature ─────────────────────────────────────────────────────────────

    private static (string salt, string sign) GenerateAuth(string signKey)
    {
        var ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;
        var salt = Convert.ToHexString(
                       SHA1.HashData(Encoding.UTF8.GetBytes(ts.ToString("F4", CultureInfo.InvariantCulture))))
                   .ToLowerInvariant();
        var sign = Convert.ToHexString(
                       HMACSHA512.HashData(Encoding.UTF8.GetBytes(signKey), Encoding.UTF8.GetBytes(salt)))
                   .ToLowerInvariant();
        return (salt, sign);
    }

    // ── HTTP ──────────────────────────────────────────────────────────────────

    private async Task<string> PostAsync(string xml, string baseUrl, CancellationToken ct)
    {
        using var http = httpClientFactory.CreateClient(HttpClientName);
        var content = new FormUrlEncodedContent([new KeyValuePair<string, string>("data", xml)]);

        var response = await http.PostAsync(baseUrl, content, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("[iPay ←] HTTP {Status}: {Body}", (int)response.StatusCode, body);
            throw new HttpRequestException($"iPay HTTP {(int)response.StatusCode}: {body}");
        }
        return body;
    }

    // ── Parsing ─────────────────────────────────────────────────────────────────

    private static XElement? TryParseRoot(string xml)
    {
        try { return XDocument.Parse(xml).Root; }
        catch { return null; }
    }

    private static (string? pid, string? url) ParseCreateResponse(string xml)
    {
        var root = TryParseRoot(xml);
        var pid = root?.Element("pid")?.Value;
        var url = root?.Element("url")?.Value;
        return (string.IsNullOrEmpty(pid) ? null : pid, string.IsNullOrEmpty(url) ? null : url);
    }

    // iPay status codes: 1=Registered, 3=Authorized, 4=Failed, 5=Success, 9=Cancelled.
    private static ProviderPaymentStatus MapStatusCode(string? raw) =>
        int.TryParse(raw, out var c)
            ? c switch
            {
                5 => ProviderPaymentStatus.Succeeded,
                4 => ProviderPaymentStatus.Failed,
                9 => ProviderPaymentStatus.Cancelled,
                1 or 3 => ProviderPaymentStatus.Pending,
                _ => ProviderPaymentStatus.Unknown,
            }
            : ProviderPaymentStatus.Unknown;
}
