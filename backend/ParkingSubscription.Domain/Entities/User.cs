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

    public string? Name { get; set; }
    public string? Surname { get; set; }
    public string? FirstName { get; set; }
    public string? Email { get; set; }

    public bool IsBlocked { get; set; }
    public bool IsSuspended { get; set; }
    public AnonymizationState AnonymizationState { get; set; } = AnonymizationState.None;

    public ICollection<ParkingCard> ParkingCards { get; set; } = new List<ParkingCard>();
    public ICollection<ValueCard> ValueCards { get; set; } = new List<ValueCard>();
}
