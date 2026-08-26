using ParkingSubscription.Domain.Common;

namespace ParkingSubscription.Domain.Entities;

/// <summary>
/// A parking-system client. In the B2C flow a Customer is created 1:1 with a
/// single <see cref="User"/> at registration, but the model supports 1:many
/// for future B2B use (ТЗ §1, §3, §10.1).
/// </summary>
public class Customer : Entity
{
    /// <summary>Correlation id in an external CRM / contact system (ТЗ §4.1 search).</summary>
    public string? ExternalContactId { get; set; }

    /// <summary>
    /// UUID assigned by SKIDATA sweb when this customer was created there. Null until
    /// the outbox has propagated the Create; used to target update/block/anonymize.
    /// </summary>
    public Guid? SkidataCustomerId { get; set; }

    public string? Name { get; set; }
    public string? Surname { get; set; }
    public string? FirstName { get; set; }
    public string? Email { get; set; }

    /// <summary>Mobile phone number (sweb Contact.mobile).</summary>
    public string? Mobile { get; set; }

    public bool IsBlocked { get; set; }

    public ICollection<User> Users { get; set; } = new List<User>();
}
