using ParkingSubscription.Domain.Common;
using ParkingSubscription.Domain.Enums;

namespace ParkingSubscription.Domain.Entities;

/// <summary>
/// A top-up / value card owned by a user (ТЗ §1, §4.2). The full operation set
/// is an open question (ТЗ §10.2); this is the CRUD skeleton.
/// </summary>
public class ValueCard : Entity
{
    public Guid UserId { get; set; }
    public User? User { get; set; }

    public string? ExternalCardId { get; set; }

    /// <summary>Stored balance in minor units (e.g. cents) to avoid float rounding.</summary>
    public long BalanceMinor { get; set; }
    public string Currency { get; set; } = "UAH";

    public CardStatus Status { get; set; } = CardStatus.Active;
}
