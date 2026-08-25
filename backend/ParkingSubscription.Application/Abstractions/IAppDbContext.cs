using Microsoft.EntityFrameworkCore;
using ParkingSubscription.Domain.Entities;

namespace ParkingSubscription.Application.Abstractions;

/// <summary>
/// Persistence abstraction the application layer depends on; implemented by the
/// EF Core <c>AppDbContext</c> in Infrastructure. Keeps services free of a hard
/// dependency on a concrete DbContext.
/// </summary>
public interface IAppDbContext
{
    DbSet<Customer> Customers { get; }
    DbSet<User> Users { get; }
    DbSet<ParkingCard> ParkingCards { get; }
    DbSet<ValueCard> ValueCards { get; }
    DbSet<AppAccount> AppAccounts { get; }
    DbSet<LoginOtp> LoginOtps { get; }
    DbSet<OutboxMessage> OutboxMessages { get; }
    DbSet<AuditLogEntry> AuditLog { get; }
    DbSet<SubscriptionPlan> SubscriptionPlans { get; }
    DbSet<Payment> Payments { get; }
    DbSet<FiscalReceiptBlob> FiscalReceiptBlobs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
