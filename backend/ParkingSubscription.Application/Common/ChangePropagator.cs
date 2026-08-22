using System.Text.Json;
using ParkingSubscription.Application.Abstractions;
using ParkingSubscription.Domain.Entities;
using ParkingSubscription.Domain.Enums;

namespace ParkingSubscription.Application.Common;

/// <summary>
/// Enqueues an outbox message for async propagation to Parking.Logic and writes
/// an audit record for sensitive operations (ТЗ §5, §9). Does not call
/// SaveChanges — the caller commits within its own unit of work.
/// </summary>
public sealed class ChangePropagator(IAppDbContext db)
{
    private static readonly HashSet<PropagationOperation> Audited =
    [
        PropagationOperation.Block, PropagationOperation.Unblock,
        PropagationOperation.Suspend, PropagationOperation.Resume,
        PropagationOperation.Delete, PropagationOperation.Anonymize
    ];

    public void Enqueue(EntityKind kind, Guid entityId, PropagationOperation op, object? payload = null, string actor = "system")
    {
        var payloadJson = payload is null ? null : JsonSerializer.Serialize(payload);

        db.OutboxMessages.Add(new OutboxMessage
        {
            EntityKind = kind,
            EntityId = entityId,
            Operation = op,
            PayloadJson = payloadJson
        });

        if (Audited.Contains(op))
        {
            db.AuditLog.Add(new AuditLogEntry
            {
                EntityKind = kind,
                EntityId = entityId,
                Operation = op,
                Actor = actor,
                Details = payloadJson
            });
        }
    }
}
