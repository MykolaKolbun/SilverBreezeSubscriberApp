namespace ParkingSubscription.Domain.Entities;

/// <summary>
/// Single-row configuration for the SKIDATA sweb(R) Subscribe API integration,
/// editable from the AdminPanel (no env secrets). basicAuth username/password are
/// stored ENCRYPTED at rest (shared Data Protection keys); everything else is
/// non-secret. The outbox client reads this row at runtime, so changes take effect
/// without a redeploy.
/// </summary>
public class ParkingIntegrationConfig
{
    public const int SingletonId = 1;

    public int Id { get; set; } = SingletonId;

    /// <summary>Master switch. When false the outbox client no-ops (nothing is pushed).</summary>
    public bool Enabled { get; set; }

    /// <summary>Server base URL incl. path, e.g. https://sweb.skidata.com/bei/DTASales/SubscribeApi.</summary>
    public string? BaseUrl { get; set; }

    /// <summary>basicAuth username. Encrypted.</summary>
    public string? UsernameEncrypted { get; set; }

    /// <summary>basicAuth password. Encrypted.</summary>
    public string? PasswordEncrypted { get; set; }

    /// <summary>Facility (car park) number — path parameter on all card endpoints.</summary>
    public string? FacilityNumber { get; set; }

    /// <summary>SKIDATA product UUID for standard parking cards.</summary>
    public Guid? ParkingProductId { get; set; }

    /// <summary>SKIDATA product UUID for value cards (optional).</summary>
    public Guid? ValueProductId { get; set; }

    /// <summary>ISO 3166 Alpha-2 country stamped on license plates (default UA).</summary>
    public string? DefaultCountry { get; set; } = "UA";

    /// <summary>Identification.type for the QR payload (e.g. "EXT").</summary>
    public string? QrIdentificationType { get; set; } = "EXT";

    /// <summary>
    /// Identification.subType for the QR payload. sweb's own auto-generated external
    /// identification uses subType "_SDCP" (spec §3.4.1.3.4); default to that unless
    /// SKIDATA specifies a different barcode subType.
    /// </summary>
    public string? QrIdentificationSubType { get; set; } = "_SDCP";

    /// <summary>Which CreateUser field links a user to its customer: "b2b" or "group".</summary>
    public string? CustomerLinkField { get; set; } = "b2b";

    public DateTimeOffset UpdatedAt { get; set; }
}
