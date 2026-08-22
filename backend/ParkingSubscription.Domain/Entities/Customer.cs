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

    public string? Name { get; set; }
    public string? Surname { get; set; }
    public string? FirstName { get; set; }
    public string? Email { get; set; }

    public bool IsBlocked { get; set; }

    public ICollection<User> Users { get; set; } = new List<User>();
}
