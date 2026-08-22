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

public sealed record PaymentIntent(string ProviderPaymentId, string ClientSecret);

/// <summary>Payment provider abstraction (Stripe/LiqPay/Fondy — ТЗ §6, §10.3).</summary>
public interface IPaymentProvider
{
    Task<PaymentIntent> CreatePaymentAsync(long amountMinor, string currency, string reference, CancellationToken ct = default);
    Task RefundAsync(string providerPaymentId, CancellationToken ct = default);
}

public sealed record FiscalReceipt(string ReceiptId, string Url);

/// <summary>Fiscalization provider abstraction (ТЗ §6).</summary>
public interface IFiscalProvider
{
    Task<FiscalReceipt> FiscalizeAsync(Payment payment, CancellationToken ct = default);
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
