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

    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }

    public CardStatus Status { get; set; } = CardStatus.Active;
    public AnonymizationState AnonymizationState { get; set; } = AnonymizationState.None;

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
