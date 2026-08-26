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
    public DbSet<LoginOtp> LoginOtps => Set<LoginOtp>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<AuditLogEntry> AuditLog => Set<AuditLogEntry>();
    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentGatewayConfig> PaymentGatewayConfigs => Set<PaymentGatewayConfig>();
    public DbSet<FiscalGatewayConfig> FiscalGatewayConfigs => Set<FiscalGatewayConfig>();
    public DbSet<FiscalReceiptBlob> FiscalReceiptBlobs => Set<FiscalReceiptBlob>();
    public DbSet<AdminConfig> AdminConfigs => Set<AdminConfig>();
    public DbSet<ParkingIntegrationConfig> ParkingIntegrationConfigs => Set<ParkingIntegrationConfig>();

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
            // Email and Phone are alternate login identities; each is unique only when present
            // (a phone-only account has no email and vice versa). "…" IS NOT NULL works on both
            // PostgreSQL and SQLite.
            e.HasIndex(x => x.Email).IsUnique().HasFilter("\"Email\" IS NOT NULL");
            e.HasIndex(x => x.Phone).IsUnique().HasFilter("\"Phone\" IS NOT NULL");
            e.HasIndex(x => x.RefreshTokenHash);
            e.Property(x => x.Email).HasMaxLength(320);
            e.Property(x => x.Phone).HasMaxLength(20);
            e.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
        });

        b.Entity<LoginOtp>(e =>
        {
            e.HasKey(x => x.Identifier);
            e.Property(x => x.Identifier).HasMaxLength(320).ValueGeneratedNever();
            e.Property(x => x.CodeHash).HasMaxLength(200);
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

        b.Entity<PaymentGatewayConfig>(e =>
        {
            e.Property(x => x.Id).ValueGeneratedNever(); // singleton row (id = 1)
            e.Property(x => x.MerchantId).HasMaxLength(128);
            e.Property(x => x.BaseUrl).HasMaxLength(256);
        });

        b.Entity<FiscalGatewayConfig>(e =>
        {
            e.Property(x => x.Id).ValueGeneratedNever(); // singleton row (id = 1)
            e.Property(x => x.BaseUrl).HasMaxLength(256);
        });

        b.Entity<FiscalReceiptBlob>(e =>
        {
            e.HasKey(x => x.PaymentId);
            e.Property(x => x.PaymentId).ValueGeneratedNever();
            e.Property(x => x.ContentType).HasMaxLength(64);
        });

        b.Entity<AdminConfig>(e =>
        {
            e.Property(x => x.Id).ValueGeneratedNever(); // singleton row (id = 1)
            e.Property(x => x.PasswordHash).HasMaxLength(200);
        });

        b.Entity<ParkingIntegrationConfig>(e =>
        {
            e.Property(x => x.Id).ValueGeneratedNever(); // singleton row (id = 1)
            e.Property(x => x.BaseUrl).HasMaxLength(256);
            e.Property(x => x.FacilityNumber).HasMaxLength(64);
            e.Property(x => x.DefaultCountry).HasMaxLength(2);
            e.Property(x => x.QrIdentificationType).HasMaxLength(256);
            e.Property(x => x.QrIdentificationSubType).HasMaxLength(256);
            e.Property(x => x.CustomerLinkField).HasMaxLength(16);
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
