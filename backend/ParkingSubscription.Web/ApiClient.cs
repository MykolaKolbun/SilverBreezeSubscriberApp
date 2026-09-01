using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ParkingSubscription.Web;

/// <summary>Error returned by the backend API, with a user-presentable message.</summary>
public sealed class ApiException(string message) : Exception(message);

/// <summary>
/// The single gateway to the backend API. Wraps HttpClient, attaches the JWT
/// from the server-side session, and turns ProblemDetails errors into
/// <see cref="ApiException"/> so pages can just show the message.
/// </summary>
public sealed class ApiClient(HttpClient http, IHttpContextAccessor accessor)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private ISession Session => accessor.HttpContext!.Session;

    public bool IsLoggedIn => Session.GetString("accessToken") is not null;
    public Guid UserId => Guid.Parse(Session.GetString("userId")!);

    // ---- Auth (passwordless email code, same as the mobile app) ----

    /// <summary>Step 1: request a one-time login code by email. DevCode is set only in the test phase.</summary>
    public async Task<EmailCodeResult> RequestEmailCodeAsync(string email) =>
        (await SendAsync<EmailCodeResult>(HttpMethod.Post, "/auth/email/request-code",
            new { email }, withAuth: false))!;

    /// <summary>Step 2: verify the code. Provisions the account on first login and stores the session.</summary>
    public async Task VerifyEmailCodeAsync(string email, string code)
    {
        var result = await SendAsync<AuthResult>(HttpMethod.Post, "/auth/email/verify",
            new { email, code }, withAuth: false);
        Session.SetString("accessToken", result!.AccessToken);
        Session.SetString("userId", result.UserId.ToString());
    }

    public void Logout() => Session.Clear();

    // ---- Plans & payments ----

    public async Task<List<PlanDto>> GetPlansAsync() =>
        (await SendAsync<List<PlanDto>>(HttpMethod.Get, "/plans"))!;

    /// <summary>Create the payment; RedirectUrl is the provider's hosted page (iPay) to send the browser to.</summary>
    public async Task<InitiatePaymentResult> InitiatePaymentAsync(Guid planId, DateOnly? startDate = null) =>
        (await SendAsync<InitiatePaymentResult>(HttpMethod.Post, "/payments",
            new { userId = UserId, subscriptionPlanId = planId, startDate, client = "web" }))!;

    public async Task<PaymentDto> GetPaymentAsync(Guid paymentId) =>
        (await SendAsync<PaymentDto>(HttpMethod.Get, $"/payments/{paymentId}"))!;

    // ---- My cards ----

    public async Task<PagedResult<ParkingCardDto>> GetMyCardsAsync() =>
        (await SendAsync<PagedResult<ParkingCardDto>>(HttpMethod.Get, $"/users/{UserId}/parking-cards"))!;

    public Task<byte[]> GetQrPngAsync(Guid cardId) => GetBytesAsync($"/parking-cards/{cardId}/qr");

    public Task<byte[]> GetApplePassAsync(Guid cardId) => GetBytesAsync($"/parking-cards/{cardId}/wallet/apple");

    public async Task<string> GetGoogleWalletLinkAsync(Guid cardId)
    {
        var result = await SendAsync<GoogleWalletLink>(HttpMethod.Get, $"/parking-cards/{cardId}/wallet/google");
        return result!.SaveUrl;
    }

    // ---- Profile ----

    public async Task<ApiUser> GetUserAsync() =>
        (await SendAsync<ApiUser>(HttpMethod.Get, $"/users/{UserId}"))!;

    public Task UpdateUserAsync(string? firstName, string? surname, string? mobile) =>
        SendAsync<ApiUser>(HttpMethod.Put, $"/users/{UserId}", new { firstName, surname, mobile });

    // ---- Vehicles ----

    public async Task<List<ApiVehicle>> GetVehiclesAsync() =>
        (await SendAsync<List<ApiVehicle>>(HttpMethod.Get, $"/users/{UserId}/vehicles"))!;

    public Task CreateVehicleAsync(string plateNumber, string? make, string? model) =>
        SendAsync<ApiVehicle>(HttpMethod.Post, "/vehicles",
            new { userId = UserId, plateNumber, country = "UA", make, model });

    public Task UpdateVehicleAsync(Guid id, string plateNumber, string? make, string? model) =>
        SendAsync<ApiVehicle>(HttpMethod.Put, $"/vehicles/{id}",
            new { plateNumber, country = "UA", make, model });

    public Task DeleteVehicleAsync(Guid id) =>
        SendAsync<object?>(HttpMethod.Delete, $"/vehicles/{id}");

    // ---- Payment history & receipts ----

    public async Task<List<PaymentDto>> GetPaymentsAsync() =>
        (await SendAsync<List<PaymentDto>>(HttpMethod.Get, $"/users/{UserId}/payments"))!;

    public Task<byte[]> GetReceiptPngAsync(Guid paymentId) => GetBytesAsync($"/payments/{paymentId}/receipt");

    public Task<byte[]> GetReceiptPdfAsync(Guid paymentId) => GetBytesAsync($"/payments/{paymentId}/receipt.pdf");

    // ---- Plumbing ----

    private async Task<T?> SendAsync<T>(HttpMethod method, string url, object? body = null, bool withAuth = true)
    {
        using var response = await SendRawAsync(method, url, body, withAuth);
        if (response.Content.Headers.ContentLength is 0 or null)
            return default;
        return await response.Content.ReadFromJsonAsync<T>(Json);
    }

    private async Task<byte[]> GetBytesAsync(string url)
    {
        using var response = await SendRawAsync(HttpMethod.Get, url, body: null, withAuth: true);
        return await response.Content.ReadAsByteArrayAsync();
    }

    private async Task<HttpResponseMessage> SendRawAsync(HttpMethod method, string url, object? body, bool withAuth)
    {
        using var request = new HttpRequestMessage(method, url);

        if (withAuth && Session.GetString("accessToken") is string token)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        if (body is not null)
            request.Content = new StringContent(JsonSerializer.Serialize(body, Json), Encoding.UTF8, "application/json");

        var response = await http.SendAsync(request);
        if (response.IsSuccessStatusCode)
            return response;

        if (response.StatusCode == HttpStatusCode.Unauthorized && withAuth)
        {
            Session.Clear(); // token expired or invalid — force a fresh login
            response.Dispose();
            throw new ApiException("Сесія завершилася. Будь ласка, увійдіть знову.");
        }

        // The API returns RFC7807 ProblemDetails; surface its title.
        string message = "The request failed.";
        try
        {
            var problem = await response.Content.ReadFromJsonAsync<ProblemDto>(Json);
            if (!string.IsNullOrWhiteSpace(problem?.Title))
                message = problem.Title;
        }
        catch (JsonException) { /* non-JSON error body — keep the generic message */ }

        response.Dispose();
        throw new ApiException(message);
    }
}

// ---- API response shapes (only the fields the pages actually use) ----

public sealed record EmailCodeResult(string Email, string? DevCode);
public sealed record AuthResult(string AccessToken, string RefreshToken, DateTimeOffset AccessTokenExpiresAt, Guid UserId, Guid CustomerId);
public sealed record PlanDto(Guid Id, string Code, string Name, long PriceMinor, string Currency, int DurationDays);
public sealed record InitiatePaymentResult(Guid PaymentId, string ProviderPaymentId, string RedirectUrl, long AmountMinor, string Currency);
public sealed record PaymentDto(
    Guid Id, Guid? ParkingCardId, long AmountMinor, string Currency, string Status,
    string? FiscalReceiptId, string? FiscalReceiptUrl, string? FailureReason, DateTimeOffset UpdatedAt);
public sealed record ApiUser(Guid Id, string? FirstName, string? Surname, string? Email, string? Mobile);
public sealed record ApiVehicle(Guid Id, string PlateNumber, string Country, string? Make, string? Model);
public sealed record ParkingCardDto(Guid Id, DateOnly StartDate, DateOnly EndDate, string Status, string QrPayload);
public sealed record PagedResult<T>(List<T> Items, string? NextPagingToken);
public sealed record GoogleWalletLink(string SaveUrl);
public sealed record ProblemDto(string? Title);

public static class Money
{
    /// <summary>Formats minor units (kopiykas/cents) as a human amount, e.g. 90000 → "900 UAH".</summary>
    public static string Format(long amountMinor, string currency) => $"{amountMinor / 100m:0.##} {currency}";
}
