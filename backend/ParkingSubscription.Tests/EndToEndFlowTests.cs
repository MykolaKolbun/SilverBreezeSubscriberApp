using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ParkingSubscription.Application.Auth;
using ParkingSubscription.Application.Facade;
using ParkingSubscription.Application.Payments;
using ParkingSubscription.Domain.Enums;
using Xunit;

namespace ParkingSubscription.Tests;

public sealed class EndToEndFlowTests(TestWebAppFactory factory) : IClassFixture<TestWebAppFactory>
{
    private readonly HttpClient _client = factory.CreateClient();
    private static readonly System.Text.Json.JsonSerializerOptions Json = TestWebAppFactory.Json;

    [Fact]
    public async Task Register_Confirm_Login_Buy_Fiscalize_Qr_flow_works()
    {
        var email = $"user-{Guid.NewGuid():N}@example.com";

        // 1. Register → provisions Customer + User 1:1, returns dev confirmation token.
        var register = await PostAsync<RegisterResult>("/auth/register",
            new RegisterRequest(email, "Sup3rSecret!", "Test", "User"));
        Assert.NotEqual(Guid.Empty, register.UserId);
        Assert.NotEqual(Guid.Empty, register.CustomerId);
        Assert.False(string.IsNullOrEmpty(register.DevConfirmationToken));

        // 2. Confirm email.
        var confirm = await _client.PostAsJsonAsync("/auth/confirm-email",
            new ConfirmEmailRequest(email, register.DevConfirmationToken!));
        Assert.Equal(HttpStatusCode.NoContent, confirm.StatusCode);

        // 3. Login → JWT.
        var auth = await PostAsync<AuthResult>("/auth/login", new LoginRequest(email, "Sup3rSecret!"));
        Assert.Equal(register.UserId, auth.UserId);
        Assert.Equal(register.CustomerId, auth.CustomerId);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        // 4. Pick a seeded plan.
        var plans = await GetAsync<List<PlanDto>>("/plans");
        Assert.NotEmpty(plans);
        var plan = plans[0];

        // 5. Initiate payment, then simulate provider "succeeded" webhook.
        var init = await PostAsync<InitiatePaymentResult>("/payments",
            new InitiatePaymentRequest(register.UserId, plan.Id, null));
        Assert.Equal(plan.PriceMinor, init.AmountMinor);

        var payment = await PostAsync<PaymentDto>("/payments/webhook",
            new PaymentWebhookRequest(init.ProviderPaymentId, "succeeded"));
        Assert.Equal(nameof(PaymentStatus.Succeeded), payment.Status);
        Assert.NotNull(payment.ParkingCardId);       // card activated
        Assert.False(string.IsNullOrEmpty(payment.FiscalReceiptId)); // fiscalized

        // 6. QR for the activated card is a PNG.
        var qr = await _client.GetAsync($"/parking-cards/{payment.ParkingCardId}/qr");
        Assert.Equal(HttpStatusCode.OK, qr.StatusCode);
        Assert.Equal("image/png", qr.Content.Headers.ContentType!.MediaType);
        Assert.True((await qr.Content.ReadAsByteArrayAsync()).Length > 0);

        // 7. Apple Wallet pass is downloadable.
        var apple = await _client.GetAsync($"/parking-cards/{payment.ParkingCardId}/wallet/apple");
        Assert.Equal(HttpStatusCode.OK, apple.StatusCode);
        Assert.Equal("application/vnd.apple.pkpass", apple.Content.Headers.ContentType!.MediaType);
    }

    [Fact]
    public async Task Email_otp_provisions_account_and_logs_in()
    {
        var email = $"otp-{Guid.NewGuid():N}@example.com";

        // 1. Request a code — dev code is returned because Auth:ExposeDevTokens is on in tests.
        var req = await PostAsync<EmailCodeResp>("/auth/email/request-code", new { email });
        Assert.False(string.IsNullOrEmpty(req.DevCode));
        Assert.Equal(email, req.Email);

        // 2. Verify → provisions Customer+User on first login and issues JWTs.
        var auth = await PostAsync<AuthResult>("/auth/email/verify", new { email, code = req.DevCode });
        Assert.NotEqual(Guid.Empty, auth.UserId);
        Assert.NotEqual(Guid.Empty, auth.CustomerId);

        // 3. The access token works on a protected endpoint.
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        var plans = await GetAsync<List<PlanDto>>("/plans");
        Assert.NotEmpty(plans);

        // 4. A wrong/expired code is rejected.
        var bad = await _client.PostAsJsonAsync("/auth/email/verify", new { email, code = "000000" });
        Assert.Equal(HttpStatusCode.Unauthorized, bad.StatusCode);
    }

    private sealed record EmailCodeResp(string Email, string? DevCode);

    [Fact]
    public async Task Second_active_card_in_same_period_is_rejected()
    {
        var (userId, _) = await RegisterAndLoginAsync();

        var start = DateOnly.FromDateTime(DateTime.UtcNow);
        var end = start.AddDays(30);
        var first = await _client.PostAsJsonAsync("/parking-cards",
            new CreateParkingCardRequest(userId, null, start, end, null));
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        // Overlapping period → 409 Conflict (ТЗ §5 one active card per period).
        var second = await _client.PostAsJsonAsync("/parking-cards",
            new CreateParkingCardRequest(userId, null, start.AddDays(10), end.AddDays(10), null));
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Block_card_records_audit_and_outbox()
    {
        var (userId, _) = await RegisterAndLoginAsync();
        var start = DateOnly.FromDateTime(DateTime.UtcNow);
        var card = await PostAsync<ParkingCardDto>("/parking-cards",
            new CreateParkingCardRequest(userId, null, start, start.AddDays(30), null));

        var block = await _client.PostAsync($"/parking-cards/{card.Id}/block", null);
        Assert.Equal(HttpStatusCode.NoContent, block.StatusCode);

        var reread = await GetAsync<ParkingCardDto>($"/parking-cards/{card.Id}");
        Assert.Equal(CardStatus.Blocked, reread.Status);
    }

    [Fact]
    public async Task Unauthenticated_facade_call_is_rejected()
    {
        using var anon = factory.CreateClient();
        var resp = await anon.GetAsync("/customers/changes");
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task Health_endpoint_is_healthy()
    {
        using var anon = factory.CreateClient();
        var resp = await anon.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    private async Task<(Guid userId, string accessToken)> RegisterAndLoginAsync()
    {
        var email = $"user-{Guid.NewGuid():N}@example.com";
        var register = await PostAsync<RegisterResult>("/auth/register",
            new RegisterRequest(email, "Sup3rSecret!", "T", "U"));
        await _client.PostAsJsonAsync("/auth/confirm-email",
            new ConfirmEmailRequest(email, register.DevConfirmationToken!));
        var auth = await PostAsync<AuthResult>("/auth/login", new LoginRequest(email, "Sup3rSecret!"));
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return (register.UserId, auth.AccessToken);
    }

    private async Task<T> PostAsync<T>(string url, object body)
    {
        var resp = await _client.PostAsJsonAsync(url, body);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<T>(Json))!;
    }

    private async Task<T> GetAsync<T>(string url)
    {
        var resp = await _client.GetAsync(url);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<T>(Json))!;
    }
}
