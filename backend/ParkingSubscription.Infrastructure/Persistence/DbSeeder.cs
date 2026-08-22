using Microsoft.EntityFrameworkCore;
using ParkingSubscription.Domain.Entities;

namespace ParkingSubscription.Infrastructure.Persistence;

/// <summary>Seeds baseline subscription tariffs (ТЗ §2.3, §10.5).</summary>
public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db, CancellationToken ct = default)
    {
        if (await db.SubscriptionPlans.AnyAsync(ct))
            return;

        db.SubscriptionPlans.AddRange(
            new SubscriptionPlan { Code = "MONTHLY", Name = "Monthly parking", PriceMinor = 90000, Currency = "UAH", DurationDays = 30 },
            new SubscriptionPlan { Code = "QUARTERLY", Name = "Quarterly parking", PriceMinor = 240000, Currency = "UAH", DurationDays = 90 },
            new SubscriptionPlan { Code = "ANNUAL", Name = "Annual parking", PriceMinor = 850000, Currency = "UAH", DurationDays = 365 });

        await db.SaveChangesAsync(ct);
    }
}
