using ParkingSubscription.Domain.Entities;
using ParkingSubscription.Domain.Enums;

namespace ParkingSubscription.Application.Abstractions;

/// <summary>Abstracts the current time for deterministic testing.</summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
    DateOnly Today { get; }
}

/// <summary>Password hashing/verification (bcrypt/argon2 per ТЗ §9).</summary>
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);
}

public sealed record AccessTokens(string AccessToken, string RefreshToken, DateTimeOffset AccessTokenExpiresAt);

/// <summary>Issues and validates JWT access/refresh tokens (ТЗ §3).</summary>
public interface IJwtTokenService
{
    AccessTokens Issue(AppAccount account);
    string CreateRefreshToken();
    string HashRefreshToken(string refreshToken);
}

/// <summary>
/// Facade over the future Parking.Logic external API (ТЗ §4). All methods are
/// stubbed for now; they return external ids and never throw so local state
/// stays the source of truth.
/// </summary>
public interface IParkingLogicClient
{
    Task<string> PropagateAsync(EntityKind kind, Guid entityId, PropagationOperation op, string? payloadJson, CancellationToken ct = default);
}

/// <summary>Input for creating a hosted-page payment at the provider (iPay).</summary>
public sealed record PaymentInitiation(
    /// <summary>Amount in minor units (kopiykas).</summary>
    long AmountMinor,
    /// <summary>ISO 4217 currency (e.g. "UAH").</summary>
    string Currency,
    /// <summary>Our internal reference (the Payment id) echoed back by the provider.</summary>
    string Reference,
    /// <summary>Human-readable description shown on the payment page.</summary>
    string Description,
    /// <summary>Provider redirects the browser here after success.</summary>
    string SuccessUrl,
    /// <summary>Provider redirects the browser here after failure/cancel.</summary>
    string FailureUrl);

/// <summary>Result of creating a payment: the provider id and the hosted page URL.</summary>
public sealed record PaymentIntent(string ProviderPaymentId, string RedirectUrl);

/// <summary>Normalised provider payment status. The provider is the source of truth.</summary>
public enum ProviderPaymentStatus { Pending, Succeeded, Failed, Cancelled, Unknown }

/// <summary>Authoritative status fetched server-side from the provider by payment id.</summary>
public sealed record ProviderPaymentStatusResult(ProviderPaymentStatus Status, long AmountMinor);

/// <summary>Payment provider abstraction (iPay in production, stub in tests/dev — ТЗ §6, §10.3).</summary>
public interface IPaymentProvider
{
    /// <summary>Creates a hosted-page payment and returns the URL to open in a browser.</summary>
    Task<PaymentIntent> CreatePaymentAsync(PaymentInitiation initiation, CancellationToken ct = default);

    /// <summary>Fetches the authoritative payment status from the provider (never trust the client).</summary>
    Task<ProviderPaymentStatusResult> GetStatusAsync(string providerPaymentId, CancellationToken ct = default);

    Task RefundAsync(string providerPaymentId, CancellationToken ct = default);
}

/// <summary>Encrypts/decrypts secrets at rest (e.g. the payment SignKey).</summary>
public interface ICredentialProtector
{
    string Protect(string plaintext);
    string Unprotect(string ciphertext);
}

public sealed record FiscalReceipt(string ReceiptId, string Url);

/// <summary>Rendered fiscal receipt image (Checkbox /receipts/{id}/png).</summary>
public sealed record FiscalReceiptImage(byte[] Content, string ContentType);

/// <summary>
/// Fiscalization provider abstraction (ТЗ §6). Swappable via config — Stub for
/// dev/tests, Checkbox Online in production.
/// </summary>
public interface IFiscalProvider
{
    /// <summary>Fiscalizes a paid payment and returns the receipt id + tax URL.</summary>
    Task<FiscalReceipt> FiscalizeAsync(Payment payment, CancellationToken ct = default);

    /// <summary>Fetches the rendered receipt image (PNG) by receipt id; null if unavailable.</summary>
    Task<FiscalReceiptImage?> GetReceiptImageAsync(string receiptId, CancellationToken ct = default);

    /// <summary>Fetches the receipt as a PDF by receipt id; null if unavailable.</summary>
    Task<FiscalReceiptImage?> GetReceiptPdfAsync(string receiptId, CancellationToken ct = default);
}

public sealed record WalletPass(byte[] Content, string ContentType, string FileName);

/// <summary>Apple Wallet (.pkpass) / Google Wallet pass generation (ТЗ §7).</summary>
public interface IWalletPassService
{
    WalletPass BuildApplePass(ParkingCard card);
    string BuildGoogleWalletLink(ParkingCard card);
    /// <summary>Push an update to already-issued passes when card status changes (ТЗ §7).</summary>
    Task PushPassUpdateAsync(ParkingCard card, CancellationToken ct = default);
}

/// <summary>Raw QR image generation for a parking card (ТЗ §7).</summary>
public interface IQrCodeGenerator
{
    byte[] GeneratePng(string payload);
}

/// <summary>Transactional email delivery (email confirmation, reset — ТЗ §3, §9).</summary>
public interface IEmailSender
{
    Task SendAsync(string toEmail, string subject, string body, CancellationToken ct = default);
}

/// <summary>Push notifications for payment/card status changes (ТЗ §9).</summary>
public interface IPushSender
{
    Task SendAsync(Guid userId, string title, string body, CancellationToken ct = default);
}

/// <summary>SMS delivery for phone OTP (ТЗ §3). Stub in dev; real provider in prod.</summary>
public interface ISmsSender
{
    Task SendAsync(string phoneE164, string message, CancellationToken ct = default);
}
