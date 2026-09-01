using Microsoft.EntityFrameworkCore;
using ParkingSubscription.Application.Abstractions;
using ParkingSubscription.Application.Payments;

namespace ParkingSubscription.Application.Facade;

public sealed record PlanDto(Guid Id, string Code, string Name, long PriceMinor, string Currency, int DurationDays);

/// <summary>Computed validity window for a plan starting on a given date (backend is the source of truth).</summary>
public sealed record PlanPeriodDto(DateOnly StartDate, DateOnly EndDate);

public interface IPlanService
{
    Task<IReadOnlyList<PlanDto>> GetActiveAsync(CancellationToken ct = default);

    /// <summary>The subscription window for <paramref name="planId"/> starting on <paramref name="start"/>.</summary>
    Task<PlanPeriodDto?> GetPeriodAsync(Guid planId, DateOnly start, CancellationToken ct = default);
}

/// <summary>Lists active subscription tariffs for the buy flow (ТЗ §2.3, §10.5).</summary>
public sealed class PlanService(IAppDbContext db) : IPlanService
{
    public async Task<IReadOnlyList<PlanDto>> GetActiveAsync(CancellationToken ct = default) =>
        await db.SubscriptionPlans.AsNoTracking()
            .Where(p => p.IsActive && !p.IsDeleted)
            .OrderBy(p => p.PriceMinor)
            .Select(p => new PlanDto(p.Id, p.Code, p.Name, p.PriceMinor, p.Currency, p.DurationDays))
            .ToListAsync(ct);

    public async Task<PlanPeriodDto?> GetPeriodAsync(Guid planId, DateOnly start, CancellationToken ct = default)
    {
        var durationDays = await db.SubscriptionPlans.AsNoTracking()
            .Where(p => p.Id == planId && p.IsActive && !p.IsDeleted)
            .Select(p => (int?)p.DurationDays)
            .FirstOrDefaultAsync(ct);
        return durationDays is int d
            ? new PlanPeriodDto(start, SubscriptionSchedule.EndDate(start, d))
            : null;
    }
}
