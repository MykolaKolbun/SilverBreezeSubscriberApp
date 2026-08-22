using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ParkingSubscription.Application.Abstractions;
using ParkingSubscription.Domain.Entities;

namespace ParkingSubscription.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : DbContext(options), IAppDbContext
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<User> Users => Set<User>();
    public DbSet<ParkingCard> ParkingCards => Set<ParkingCard>();
    public DbSet<ValueCard> ValueCards => Set<ValueCard>();
    public DbSet<AppAccount> AppAccounts => Set<AppAccount>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<AuditLogEntry> AuditLog => Set<AuditLogEntry>();
    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();
    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Customer>(e =>
        {
            e.HasIndex(x => x.ExternalContactId);
            e.HasIndex(x => x.UpdatedAt);
            e.HasMany(x => x.Users).WithOne(x => x.Customer!).HasForeignKey(x => x.CustomerId);
        });

        b.Entity<User>(e =>
        {
            e.HasIndex(x => x.ExternalContactId);
            e.HasIndex(x => x.UpdatedAt);
            e.HasIndex(x => x.CustomerId);
            e.HasMany(x => x.ParkingCards).WithOne(x => x.User!).HasForeignKey(x => x.UserId);
            e.HasMany(x => x.ValueCards).WithOne(x => x.User!).HasForeignKey(x => x.UserId);
        });

        b.Entity<ParkingCard>(e =>
        {
            e.HasIndex(x => x.ExternalCardId);
            e.HasIndex(x => x.UpdatedAt);
            e.HasIndex(x => new { x.UserId, x.Status });
            e.Property(x => x.QrPayload).HasMaxLength(256);
        });

        b.Entity<ValueCard>(e =>
        {
            e.HasIndex(x => x.ExternalCardId);
            e.HasIndex(x => x.UpdatedAt);
            e.Property(x => x.Currency).HasMaxLength(3);
        });

        b.Entity<AppAccount>(e =>
        {
            e.HasIndex(x => x.Email).IsUnique();
            e.HasIndex(x => x.RefreshTokenHash);
            e.Property(x => x.Email).HasMaxLength(320).IsRequired();
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
        });

        b.Entity<OutboxMessage>(e =>
        {
            e.HasIndex(x => x.Status);
            e.HasIndex(x => x.CreatedAt);
        });

        b.Entity<AuditLogEntry>(e => e.HasIndex(x => x.CreatedAt));

        b.Entity<SubscriptionPlan>(e =>
        {
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.Currency).HasMaxLength(3);
        });

        b.Entity<Payment>(e =>
        {
            e.HasIndex(x => x.ProviderPaymentId);
            e.HasIndex(x => x.UpdatedAt);
            e.Property(x => x.Currency).HasMaxLength(3);
        });

        // SQLite cannot ORDER BY / compare DateTimeOffset. Store all DateTimeOffset
        // values as UTC ticks (INTEGER) so ordering by UpdatedAt/CreatedAt works.
        // PostgreSQL maps DateTimeOffset to timestamptz natively, so no converter there.
        if (Database.IsSqlite())
        {
            var converter = new ValueConverter<DateTimeOffset, long>(
                v => v.UtcTicks,
                v => new DateTimeOffset(v, TimeSpan.Zero));

            foreach (var entityType in b.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(DateTimeOffset) || property.ClrType == typeof(DateTimeOffset?))
                        property.SetValueConverter(converter);
                }
            }
        }
    }
}
