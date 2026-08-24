using Microsoft.EntityFrameworkCore;
using ParkingSubscription.Domain.Entities;

namespace ParkingSubscription.Infrastructure.Persistence;

/// <summary>Seeds/updates the SilverBreeze subscription tariffs (ТЗ §2.3, §10.5).</summary>
public static class DbSeeder
{
    // Prices are in minor units (kopiykas): UAH × 100.
    private static readonly (string Code, string Name, long PriceMinor, int DurationDays)[] Desired =
    [
        ("PARK_1M", "Паркінг · 1 місяць", 360000, 30),
        ("PARK_2M", "Паркінг · 2 місяці", 720000, 60),
        ("PARK_3M", "Паркінг · 3 місяці", 1080000, 90),
        ("OUT_1M", "Зовнішній паркінг · 1 місяць", 300000, 30),
        ("OUT_3M", "Зовнішній паркінг · 3 місяці", 900000, 90),
    ];

    public static async Task SeedAsync(AppDbContext db, CancellationToken ct = default)
    {
        var existing = await db.SubscriptionPlans.ToListAsync(ct);
        var desiredCodes = Desired.Select(d => d.Code).ToHashSet();

        // Upsert the desired plans by Code.
        foreach (var (code, name, priceMinor, durationDays) in Desired)
        {
            var plan = existing.FirstOrDefault(p => p.Code == code);
            if (plan is null)
            {
                db.SubscriptionPlans.Add(new SubscriptionPlan
                {
                    Code = code,
                    Name = name,
                    PriceMinor = priceMinor,
                    Currency = "UAH",
                    DurationDays = durationDays,
                    IsActive = true,
                });
            }
            else
            {
                plan.Name = name;
                plan.PriceMinor = priceMinor;
                plan.Currency = "UAH";
                plan.DurationDays = durationDays;
                plan.IsActive = true;
                plan.Touch();
            }
        }

        // Deactivate any legacy plans not in the desired set (FK-safe — never deleted).
        foreach (var plan in existing.Where(p => !desiredCodes.Contains(p.Code) && p.IsActive))
        {
            plan.IsActive = false;
            plan.Touch();
        }

        await db.SaveChangesAsync(ct);
    }
}
