using ParkingSubscription.Domain.Common;
using ParkingSubscription.Domain.Enums;

namespace ParkingSubscription.Domain.Entities;

/// <summary>
/// Transactional-outbox record used to asynchronously propagate state changes
/// (block/unblock/suspend/resume/delete/anonymize) to Parking.Logic with
/// delivery tracking and retries (ТЗ §5).
/// </summary>
public class OutboxMessage : Entity
{
    public EntityKind EntityKind { get; set; }
    public Guid EntityId { get; set; }
    public PropagationOperation Operation { get; set; }

    /// <summary>Optional JSON payload snapshot for the operation.</summary>
    public string? PayloadJson { get; set; }

    public OutboxStatus Status { get; set; } = OutboxStatus.Pending;
    public int Attempts { get; set; }
    public string? LastError { get; set; }
    public DateTimeOffset? DeliveredAt { get; set; }
}
