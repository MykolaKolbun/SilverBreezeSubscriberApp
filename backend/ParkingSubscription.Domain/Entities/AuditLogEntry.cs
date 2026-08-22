using ParkingSubscription.Domain.Common;
using ParkingSubscription.Domain.Enums;

namespace ParkingSubscription.Domain.Entities;

/// <summary>
/// Audit record for sensitive operations: block/unblock/suspend/resume/delete/
/// anonymize (ТЗ §9 logging &amp; audit).
/// </summary>
public class AuditLogEntry : Entity
{
    public EntityKind EntityKind { get; set; }
    public Guid EntityId { get; set; }
    public PropagationOperation Operation { get; set; }

    /// <summary>Who performed the action (account id / "system").</summary>
    public string Actor { get; set; } = "system";

    public string? Details { get; set; }
}
