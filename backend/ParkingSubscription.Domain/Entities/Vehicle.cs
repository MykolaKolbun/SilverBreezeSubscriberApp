using ParkingSubscription.Domain.Common;

namespace ParkingSubscription.Domain.Entities;

/// <summary>
/// A vehicle owned by a <see cref="User"/>, identified by its license plate. Plates
/// are pushed to the parking system (SKIDATA sweb <c>licensePlates</c> / LP
/// identification) so the barrier can grant entry by number as well as by QR.
/// Maps to the sweb <c>LicensePlate</c> schema: <see cref="Country"/> + <see cref="PlateNumber"/>
/// are required there; <see cref="Make"/>/<see cref="Model"/> fold into its free-text
/// <c>vehicle</c> field.
/// </summary>
public class Vehicle : Entity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }

    /// <summary>License plate number (sweb LicensePlate.value), e.g. "AA1234BB". Max 32.</summary>
    public string PlateNumber { get; set; } = string.Empty;

    /// <summary>ISO 3166 Alpha-2 country code (sweb LicensePlate.country). Default UA.</summary>
    public string Country { get; set; } = "UA";

    /// <summary>Vehicle make/brand entered in the app, e.g. "Toyota". Max 32.</summary>
    public string? Make { get; set; }

    /// <summary>Vehicle model entered in the app, e.g. "Corolla". Max 32.</summary>
    public string? Model { get; set; }
}
