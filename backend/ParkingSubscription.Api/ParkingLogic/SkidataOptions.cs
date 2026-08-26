namespace ParkingSubscription.Api.ParkingLogic;

/// <summary>
/// Configuration for the SKIDATA sweb(R) Subscribe API integration. Non-secret
/// values (BaseUrl, FacilityNumber, product ids, identification mapping) live in
/// appsettings; the basicAuth <see cref="Username"/>/<see cref="Password"/> are
/// injected from the environment / .env and never committed.
/// Bound from configuration section <c>ParkingLogic:Skidata</c>.
/// </summary>
public sealed class SkidataOptions
{
    public const string SectionName = "ParkingLogic:Skidata";

    /// <summary>Server base URL, e.g. the staging server or an EU/US production server.</summary>
    public string? BaseUrl { get; set; }

    /// <summary>Basic-auth username (secret — from .env).</summary>
    public string? Username { get; set; }

    /// <summary>Basic-auth password (secret — from .env).</summary>
    public string? Password { get; set; }

    /// <summary>Facility (car park) number — a path parameter on all parking/value card endpoints.</summary>
    public string? FacilityNumber { get; set; }

    /// <summary>UUID of the SKIDATA product used for standard parking cards.</summary>
    public Guid? ParkingProductId { get; set; }

    /// <summary>UUID of the SKIDATA product used for value cards (optional).</summary>
    public Guid? ValueProductId { get; set; }

    /// <summary>ISO 3166 Alpha-2 country code stamped on license plates (default UA).</summary>
    public string DefaultCountry { get; set; } = "UA";

    /// <summary>
    /// Identification.type for the QR payload — must match a GenericIdentification.type
    /// configured on the product (e.g. "EXT" for an external barcode).
    /// </summary>
    public string QrIdentificationType { get; set; } = "EXT";

    /// <summary>
    /// Identification.subType for the QR payload — the barcode format provided by SKIDATA
    /// to match the product configuration (e.g. "15693"). Empty by default.
    /// </summary>
    public string QrIdentificationSubType { get; set; } = string.Empty;

    /// <summary>Identification.type for license plates (default "LP").</summary>
    public string LicensePlateIdentificationType { get; set; } = "LP";

    /// <summary>
    /// Which CreateUser field links a user to its customer: "b2b" → b2bCustomerId,
    /// "group" → groupCustomerId. SKIDATA-specific; confirm with the product config.
    /// </summary>
    public string CustomerLinkField { get; set; } = "b2b";

    /// <summary>True when enough is configured to talk to sweb (URL + credentials + facility).</summary>
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(BaseUrl)
        && !string.IsNullOrWhiteSpace(Username)
        && !string.IsNullOrWhiteSpace(Password)
        && !string.IsNullOrWhiteSpace(FacilityNumber);
}
