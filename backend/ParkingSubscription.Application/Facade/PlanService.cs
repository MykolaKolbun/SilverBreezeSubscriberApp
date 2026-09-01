using Microsoft.EntityFrameworkCore;
using ParkingSubscription.Application.Abstractions;
using ParkingSubscription.Application.Payments;
using ParkingSubscription.Domain.Enums;

namespace ParkingSubscription.Application.Facade;

public sealed record PlanDto(Guid Id, string Code, string Name, long PriceMinor, string Currency, int DurationDays);

/// <summary>Computed validity window for a plan starting on a given date (backend is the source of truth).</summary>
public sealed record PlanPeriodDto(DateOnly StartDate, DateOnly EndDate);

public interface IPlanService
{
    Task<IReadOnlyList<PlanDto>> GetActiveAsync(CancellationToken ct = default);

    /// <summary>
    /// The subscription window for <paramref name="planId"/> starting on <paramref name="start"/>,
    /// anchored to <paramref name="userId"/>'s existing chain so the preview matches the card
    /// the purchase will actually create.
    /// </summary>
    Task<PlanPeriodDto?> GetPeriodAsync(Guid userId, Guid planId, DateOnly start, CancellationToken ct = default);
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

    public async Task<PlanPeriodDto?> GetPeriodAsync(Guid userId, Guid planId, DateOnly start, CancellationToken ct = default)
    {
        var durationDays = await db.SubscriptionPlans.AsNoTracking()
            .Where(p => p.Id == planId && p.IsActive && !p.IsDeleted)
            .Select(p => (int?)p.DurationDays)
            .FirstOrDefaultAsync(ct);
        if (durationDays is not int d) return null;

        var activeCards = await db.ParkingCards.AsNoTracking()
            .Where(c => c.UserId == userId && c.Status == CardStatus.Active && !c.IsDeleted)
            .Select(c => new { c.StartDate, c.EndDate })
            .ToListAsync(ct);
        var anchorDay = SubscriptionSchedule.AnchorDay(
            activeCards.Select(c => (c.StartDate, c.EndDate)), start);

        return new PlanPeriodDto(start, SubscriptionSchedule.EndDate(start, d, anchorDay));
    }
}
