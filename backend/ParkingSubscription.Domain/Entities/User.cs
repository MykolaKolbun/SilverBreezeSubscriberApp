using ParkingSubscription.Domain.Common;
using ParkingSubscription.Domain.Enums;

namespace ParkingSubscription.Domain.Entities;

/// <summary>
/// Owner of parking cards and value cards. Belongs to exactly one
/// <see cref="Customer"/> (ТЗ §1, §4.2).
/// </summary>
public class User : Entity
{
    public Guid CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public string? ExternalContactId { get; set; }

    /// <summary>UUID assigned by SKIDATA sweb when this user was created there (null until propagated).</summary>
    public Guid? SkidataUserId { get; set; }

    public string? Name { get; set; }
    public string? Surname { get; set; }
    public string? FirstName { get; set; }
    public string? Email { get; set; }

    /// <summary>Mobile phone number (sweb Contact.mobile).</summary>
    public string? Mobile { get; set; }

    // --- License-plate entry policy (sweb UserParkingContract) ---

    /// <summary>Entry/exit verified exclusively via the vehicle license plate (sweb passageLP).</summary>
    public bool PassageLp { get; set; }

    /// <summary>License plate verified together with card data at the gates (sweb checkLP).</summary>
    public bool CheckLp { get; set; }

    /// <summary>Must exit with the same plate scanned on entry (sweb matchEntryPlate).</summary>
    public bool MatchEntryPlate { get; set; }

    public bool IsBlocked { get; set; }
    public bool IsSuspended { get; set; }
    public AnonymizationState AnonymizationState { get; set; } = AnonymizationState.None;

    public ICollection<ParkingCard> ParkingCards { get; set; } = new List<ParkingCard>();
    public ICollection<ValueCard> ValueCards { get; set; } = new List<ValueCard>();
    public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
}
