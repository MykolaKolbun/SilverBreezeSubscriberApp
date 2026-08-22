namespace ParkingSubscription.Domain.Common;

/// <summary>
/// Base class for all persisted entities. Uses GUID identifiers, tracks
/// create/update timestamps and supports soft-delete (ТЗ §5, §9).
/// </summary>
public abstract class Entity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public bool IsDeleted { get; set; }

    public void Touch() => UpdatedAt = DateTimeOffset.UtcNow;
}
