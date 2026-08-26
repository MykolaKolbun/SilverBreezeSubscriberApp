using Microsoft.EntityFrameworkCore;
using ParkingSubscription.Application.Abstractions;

namespace ParkingSubscription.Application.Facade;

public sealed record PlanDto(Guid Id, string Code, string Name, long PriceMinor, string Currency, int DurationDays);

public interface IPlanService
{
    Task<IReadOnlyList<PlanDto>> GetActiveAsync(CancellationToken ct = default);
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
}
