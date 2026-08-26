using ParkingSubscription.Domain.Common;
using ParkingSubscription.Domain.Enums;

namespace ParkingSubscription.Domain.Entities;

/// <summary>
/// A contract parking subscription (ТЗ §4.3, §5, §7). A user may hold only one
/// active card per overlapping period.
/// </summary>
public class ParkingCard : Entity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }

    public string? ExternalCardId { get; set; }

    /// <summary>UUID assigned by SKIDATA sweb when this card was created there (null until propagated).</summary>
    public Guid? SkidataCardId { get; set; }

    /// <summary>Reference to the purchased tariff (ТЗ §10.5).</summary>
    public Guid? SubscriptionPlanId { get; set; }
    public SubscriptionPlan? SubscriptionPlan { get; set; }

    /// <summary>SKIDATA product UUID this card was issued against (sweb productId).</summary>
    public Guid? ProductId { get; set; }

    /// <summary>Human-readable product name from sweb (sweb productName).</summary>
    public string? ProductName { get; set; }

    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }

    public CardStatus Status { get; set; } = CardStatus.Active;
    public AnonymizationState AnonymizationState { get; set; } = AnonymizationState.None;

    /// <summary>Granted single neutral access permission (sweb singleNeutral).</summary>
    public bool SingleNeutral { get; set; }

    /// <summary>Card was canceled in the parking system (sweb GetParkingCard.canceled).</summary>
    public bool Canceled { get; set; }

    /// <summary>Date the card gets blocked in the parking system (sweb blockDate).</summary>
    public DateOnly? BlockDate { get; set; }

    /// <summary>Production reason from sweb (new vs extended ticket).</summary>
    public CardProductionReason? ProductionReason { get; set; }

    /// <summary>Suspension window (sweb Suspension.startDate/endDate); null when not suspended.</summary>
    public DateOnly? SuspensionStartDate { get; set; }
    public DateOnly? SuspensionEndDate { get; set; }

    /// <summary>Car parks this card is valid for, with entry permission (sweb carParks).</summary>
    public ICollection<CardCarPark> CarParks { get; set; } = new List<CardCarPark>();

    /// <summary>Secondary identifications on the card (sweb secondaryIds).</summary>
    public ICollection<CardIdentification> SecondaryIds { get; set; } = new List<CardIdentification>();

    /// <summary>Payload encoded into the QR code — the card identifier (ТЗ §7).</summary>
    public string QrPayload { get; set; } = string.Empty;

    /// <summary>
    /// True while the card is active and the current date falls within its period.
    /// Used to enforce the "one active card per period" rule (ТЗ §5).
    /// </summary>
    public bool IsActiveOn(DateOnly date) =>
        Status == CardStatus.Active && !IsDeleted && StartDate <= date && date <= EndDate;

    /// <summary>True when this card's period overlaps another [start, end] period.</summary>
    public bool OverlapsPeriod(DateOnly start, DateOnly end) =>
        StartDate <= end && start <= EndDate;
}
