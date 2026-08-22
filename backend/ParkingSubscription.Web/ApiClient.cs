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

    // ---- Auth ----

    public async Task<RegisterResult> RegisterAsync(string email, string password, string? firstName, string? surname) =>
        (await SendAsync<RegisterResult>(HttpMethod.Post, "/auth/register",
            new { email, password, firstName, surname }, withAuth: false))!;

    public Task ConfirmEmailAsync(string email, string token) =>
        SendAsync<object?>(HttpMethod.Post, "/auth/confirm-email", new { email, token }, withAuth: false);

    public async Task LoginAsync(string email, string password)
    {
        var result = await SendAsync<AuthResult>(HttpMethod.Post, "/auth/login",
            new { email, password }, withAuth: false);
        Session.SetString("accessToken", result!.AccessToken);
        Session.SetString("userId", result.UserId.ToString());
    }

    public void Logout() => Session.Clear();

    // ---- Plans & payments ----

    public async Task<List<PlanDto>> GetPlansAsync() =>
        (await SendAsync<List<PlanDto>>(HttpMethod.Get, "/plans"))!;

    public async Task<InitiatePaymentResult> InitiatePaymentAsync(Guid planId) =>
        (await SendAsync<InitiatePaymentResult>(HttpMethod.Post, "/payments",
            new { userId = UserId, subscriptionPlanId = planId }))!;

    public async Task<PaymentDto> GetPaymentAsync(Guid paymentId) =>
        (await SendAsync<PaymentDto>(HttpMethod.Get, $"/payments/{paymentId}"))!;

    /// <summary>Dev-only: plays the role of the payment provider calling the webhook.</summary>
    public async Task<PaymentDto> SimulateProviderCallbackAsync(string providerPaymentId, string status) =>
        (await SendAsync<PaymentDto>(HttpMethod.Post, "/payments/webhook",
            new { providerPaymentId, status }, withAuth: false))!;

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
            throw new ApiException("Your session has expired. Please log in again.");
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

public sealed record RegisterResult(Guid UserId, Guid CustomerId, string Email, string? DevConfirmationToken);
public sealed record AuthResult(string AccessToken, string RefreshToken, DateTimeOffset AccessTokenExpiresAt, Guid UserId, Guid CustomerId);
public sealed record PlanDto(Guid Id, string Code, string Name, long PriceMinor, string Currency, int DurationDays);
public sealed record InitiatePaymentResult(Guid PaymentId, string ProviderPaymentId, string ClientSecret, long AmountMinor, string Currency);
public sealed record PaymentDto(Guid Id, Guid? ParkingCardId, long AmountMinor, string Currency, string Status, string? FiscalReceiptId, string? FailureReason);
public sealed record ParkingCardDto(Guid Id, DateOnly StartDate, DateOnly EndDate, string Status, string QrPayload);
public sealed record PagedResult<T>(List<T> Items, string? NextPagingToken);
public sealed record GoogleWalletLink(string SaveUrl);
public sealed record ProblemDto(string? Title);

public static class Money
{
    /// <summary>Formats minor units (kopiykas/cents) as a human amount, e.g. 90000 → "900 UAH".</summary>
    public static string Format(long amountMinor, string currency) => $"{amountMinor / 100m:0.##} {currency}";
}
