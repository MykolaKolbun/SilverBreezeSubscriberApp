namespace ParkingSubscription.Domain.Entities;

/// <summary>
/// Single-row configuration for the fiscalization gateway (Checkbox Online).
/// PIN code and license key are stored ENCRYPTED at rest; BaseUrl and TaxCode are
/// non-secret. Seeded from Fiscal__Checkbox__* environment variables on startup.
/// </summary>
public class FiscalGatewayConfig
{
    public const int SingletonId = 1;

    public int Id { get; set; } = SingletonId;

    /// <summary>Cashier PIN code (signinPinCode). Encrypted.</summary>
    public string? PinCodeEncrypted { get; set; }

    /// <summary>Checkbox license key (X-License-Key header). Encrypted.</summary>
    public string? LicenseKeyEncrypted { get; set; }

    /// <summary>API base URL, e.g. https://api.checkbox.ua (no trailing slash).</summary>
    public string? BaseUrl { get; set; }

    /// <summary>Checkbox tax rate code applied to the parking good (null = no tax line).</summary>
    public int? TaxCode { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
