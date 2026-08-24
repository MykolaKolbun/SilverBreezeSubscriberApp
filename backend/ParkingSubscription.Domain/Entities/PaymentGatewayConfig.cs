namespace ParkingSubscription.Domain.Entities;

/// <summary>
/// Single-row configuration for the active payment gateway (iPay).
/// The application is single-merchant (B2C), so there is exactly one row, id = 1.
///
/// <see cref="SignKeyEncrypted"/> is stored ENCRYPTED at rest (via
/// <c>ICredentialProtector</c> / ASP.NET Data Protection). <see cref="MerchantId"/>
/// and <see cref="BaseUrl"/> are non-secret. The plaintext SignKey is never
/// returned by the API.
/// </summary>
public class PaymentGatewayConfig
{
    /// <summary>Fixed singleton id.</summary>
    public const int SingletonId = 1;

    public int Id { get; set; } = SingletonId;

    /// <summary>Merchant identifier (mch_id for iPay). Not a secret.</summary>
    public string? MerchantId { get; set; }

    /// <summary>Encrypted signing key (SignKey for iPay). Never returned in API responses.</summary>
    public string? SignKeyEncrypted { get; set; }

    /// <summary>
    /// Gateway base URL.
    /// iPay sandbox: https://sandbox-checkout.ipay.ua/api302
    /// iPay prod:    https://checkout.ipay.ua/api302
    /// </summary>
    public string? BaseUrl { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
