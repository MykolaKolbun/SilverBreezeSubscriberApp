namespace ParkingSubscription.Domain.Enums;

/// <summary>Lifecycle status of a parking/value card (ТЗ §4.3, §5).</summary>
public enum CardStatus
{
    Active = 0,
    Blocked = 1,
    Suspended = 2,
    Deleted = 3
}

/// <summary>Deferred anonymization state (ТЗ §4.2 anonymize, §5).</summary>
public enum AnonymizationState
{
    None = 0,
    ReadyForAnonymization = 1,
    Anonymized = 2
}

/// <summary>Payment lifecycle (ТЗ §6).</summary>
public enum PaymentStatus
{
    Pending = 0,
    Succeeded = 1,
    Declined = 2,
    TimedOut = 3,
    Refunded = 4
}

/// <summary>Outbox delivery status for async propagation to Parking.Logic (ТЗ §5).</summary>
public enum OutboxStatus
{
    Pending = 0,
    Delivered = 1,
    Failed = 2
}

/// <summary>Kind of entity an outbox/audit record targets.</summary>
public enum EntityKind
{
    Customer = 0,
    User = 1,
    ParkingCard = 2,
    ValueCard = 3
}

/// <summary>Operation to propagate to Parking.Logic.</summary>
public enum PropagationOperation
{
    Create = 0,
    Update = 1,
    Delete = 2,
    Block = 3,
    Unblock = 4,
    Suspend = 5,
    Resume = 6,
    Anonymize = 7
}
