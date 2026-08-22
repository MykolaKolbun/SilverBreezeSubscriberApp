using Microsoft.EntityFrameworkCore;
using ParkingSubscription.Application.Abstractions;
using ParkingSubscription.Application.Common;
using ParkingSubscription.Domain.Entities;
using ParkingSubscription.Domain.Enums;

namespace ParkingSubscription.Application.Facade;

public interface IParkingCardService
{
    Task<ParkingCardDto> CreateAsync(CreateParkingCardRequest req, CancellationToken ct = default);
    Task<PagedResult<ParkingCardDto>> SearchAsync(string? externalCardId, string? searchTerm, string? pagingToken, CancellationToken ct = default);
    Task<ParkingCardDto> GetAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<ParkingCardDto>> GetChangesAsync(string? pagingToken, CancellationToken ct = default);
    Task<ParkingCardDto> UpdateAsync(Guid id, UpdateParkingCardRequest req, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task AnonymizeAsync(Guid id, CancellationToken ct = default);
    Task BlockAsync(Guid id, CancellationToken ct = default);
    Task UnblockAsync(Guid id, CancellationToken ct = default);
    Task SuspendAsync(Guid id, CancellationToken ct = default);
    Task ResumeAsync(Guid id, CancellationToken ct = default);
}

/// <summary>Parking-card facade over Parking.Logic (ТЗ §4.3, §5, §7).</summary>
public sealed class ParkingCardService(
    IAppDbContext db,
    ChangePropagator propagator,
    IWalletPassService wallet,
    IClock clock) : IParkingCardService
{
    public async Task<ParkingCardDto> CreateAsync(CreateParkingCardRequest req, CancellationToken ct = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == req.UserId && !u.IsDeleted, ct)
            ?? throw new NotFoundException($"User {req.UserId} not found.");

        if (req.EndDate < req.StartDate)
            throw new ValidationException("EndDate must be on or after StartDate.");

        await EnsureNoOverlappingActiveCardAsync(user.Id, req.StartDate, req.EndDate, ct);

        var card = new ParkingCard
        {
            UserId = user.Id,
            SubscriptionPlanId = req.SubscriptionPlanId,
            ExternalCardId = req.ExternalCardId,
            StartDate = req.StartDate,
            EndDate = req.EndDate,
            Status = CardStatus.Active
        };
        card.QrPayload = card.Id.ToString("N");

        db.ParkingCards.Add(card);
        propagator.Enqueue(EntityKind.ParkingCard, card.Id, PropagationOperation.Create);
        await db.SaveChangesAsync(ct);
        return card.ToDto();
    }

    public async Task<PagedResult<ParkingCardDto>> SearchAsync(string? externalCardId, string? searchTerm, string? pagingToken, CancellationToken ct = default)
    {
        var query = db.ParkingCards.AsNoTracking().Where(c => !c.IsDeleted);
        if (!string.IsNullOrWhiteSpace(externalCardId))
            query = query.Where(c => c.ExternalCardId == externalCardId);
        // searchTerm currently unused per ТЗ §4.3.
        return await query.ToPagedAsync(pagingToken, Mapping.ToDto, ct);
    }

    public async Task<ParkingCardDto> GetAsync(Guid id, CancellationToken ct = default) =>
        (await LoadAsync(id, ct)).ToDto();

    public Task<PagedResult<ParkingCardDto>> GetChangesAsync(string? pagingToken, CancellationToken ct = default) =>
        db.ParkingCards.AsNoTracking().ToPagedAsync(pagingToken, Mapping.ToDto, ct);

    public async Task<ParkingCardDto> UpdateAsync(Guid id, UpdateParkingCardRequest req, CancellationToken ct = default)
    {
        var card = await LoadAsync(id, ct);
        var newStart = req.StartDate ?? card.StartDate;
        var newEnd = req.EndDate ?? card.EndDate;
        if (newEnd < newStart)
            throw new ValidationException("EndDate must be on or after StartDate.");

        if (req.StartDate is not null || req.EndDate is not null)
            await EnsureNoOverlappingActiveCardAsync(card.UserId, newStart, newEnd, ct, excludeCardId: card.Id);

        card.StartDate = newStart;
        card.EndDate = newEnd;
        card.ExternalCardId = req.ExternalCardId ?? card.ExternalCardId;
        card.Touch();
        propagator.Enqueue(EntityKind.ParkingCard, id, PropagationOperation.Update);
        await db.SaveChangesAsync(ct);
        return card.ToDto();
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var card = await LoadAsync(id, ct);
        card.Status = CardStatus.Deleted;
        card.IsDeleted = true;
        card.EndDate = clock.Today;
        card.Touch();
        propagator.Enqueue(EntityKind.ParkingCard, id, PropagationOperation.Delete);
        await wallet.PushPassUpdateAsync(card, ct);
        await db.SaveChangesAsync(ct);
    }

    public async Task AnonymizeAsync(Guid id, CancellationToken ct = default)
    {
        var card = await LoadAsync(id, ct, includeDeleted: true);
        card.AnonymizationState = AnonymizationState.ReadyForAnonymization;
        card.Touch();
        propagator.Enqueue(EntityKind.ParkingCard, id, PropagationOperation.Anonymize);
        await db.SaveChangesAsync(ct);
    }

    public Task BlockAsync(Guid id, CancellationToken ct = default) =>
        SetStatusAsync(id, CardStatus.Blocked, PropagationOperation.Block, ct);
    public Task UnblockAsync(Guid id, CancellationToken ct = default) =>
        SetStatusAsync(id, CardStatus.Active, PropagationOperation.Unblock, ct);
    public Task SuspendAsync(Guid id, CancellationToken ct = default) =>
        SetStatusAsync(id, CardStatus.Suspended, PropagationOperation.Suspend, ct);
    public Task ResumeAsync(Guid id, CancellationToken ct = default) =>
        SetStatusAsync(id, CardStatus.Active, PropagationOperation.Resume, ct);

    private async Task SetStatusAsync(Guid id, CardStatus status, PropagationOperation op, CancellationToken ct)
    {
        var card = await LoadAsync(id, ct);
        card.Status = status;
        card.Touch();
        propagator.Enqueue(EntityKind.ParkingCard, id, op);
        // Card status changed → push Wallet pass update (ТЗ §7).
        await wallet.PushPassUpdateAsync(card, ct);
        await db.SaveChangesAsync(ct);
    }

    /// <summary>Enforces "one active parking card per overlapping period" (ТЗ §5).</summary>
    private async Task EnsureNoOverlappingActiveCardAsync(Guid userId, DateOnly start, DateOnly end, CancellationToken ct, Guid? excludeCardId = null)
    {
        var overlaps = await db.ParkingCards.AnyAsync(c =>
            c.UserId == userId &&
            !c.IsDeleted &&
            c.Status == CardStatus.Active &&
            (excludeCardId == null || c.Id != excludeCardId) &&
            c.StartDate <= end && start <= c.EndDate, ct);

        if (overlaps)
            throw new ConflictException("User already has an active parking card in this period.");
    }

    private async Task<ParkingCard> LoadAsync(Guid id, CancellationToken ct, bool includeDeleted = false) =>
        await db.ParkingCards.FirstOrDefaultAsync(c => c.Id == id && (includeDeleted || !c.IsDeleted), ct)
        ?? throw new NotFoundException($"Parking card {id} not found.");
}
