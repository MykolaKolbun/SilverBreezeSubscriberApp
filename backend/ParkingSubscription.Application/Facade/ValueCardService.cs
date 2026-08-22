using Microsoft.EntityFrameworkCore;
using ParkingSubscription.Application.Abstractions;
using ParkingSubscription.Application.Common;
using ParkingSubscription.Domain.Entities;
using ParkingSubscription.Domain.Enums;

namespace ParkingSubscription.Application.Facade;

public interface IValueCardService
{
    Task<ValueCardDto> CreateAsync(CreateValueCardRequest req, CancellationToken ct = default);
    Task<ValueCardDto> GetAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<ValueCardDto>> GetChangesAsync(string? pagingToken, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}

/// <summary>
/// Value-card facade skeleton (ТЗ §4.2). Full operation set is an open question
/// (ТЗ §10.2); create/get/changes/delete are provided for parity.
/// </summary>
public sealed class ValueCardService(IAppDbContext db, ChangePropagator propagator) : IValueCardService
{
    public async Task<ValueCardDto> CreateAsync(CreateValueCardRequest req, CancellationToken ct = default)
    {
        var userExists = await db.Users.AnyAsync(u => u.Id == req.UserId && !u.IsDeleted, ct);
        if (!userExists)
            throw new NotFoundException($"User {req.UserId} not found.");

        var card = new ValueCard
        {
            UserId = req.UserId,
            BalanceMinor = req.BalanceMinor,
            Currency = string.IsNullOrWhiteSpace(req.Currency) ? "UAH" : req.Currency,
            ExternalCardId = req.ExternalCardId
        };
        db.ValueCards.Add(card);
        propagator.Enqueue(EntityKind.ValueCard, card.Id, PropagationOperation.Create);
        await db.SaveChangesAsync(ct);
        return card.ToDto();
    }

    public async Task<ValueCardDto> GetAsync(Guid id, CancellationToken ct = default) =>
        (await LoadAsync(id, ct)).ToDto();

    public Task<PagedResult<ValueCardDto>> GetChangesAsync(string? pagingToken, CancellationToken ct = default) =>
        db.ValueCards.AsNoTracking().ToPagedAsync(pagingToken, Mapping.ToDto, ct);

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var card = await LoadAsync(id, ct);
        card.Status = CardStatus.Deleted;
        card.IsDeleted = true;
        card.Touch();
        propagator.Enqueue(EntityKind.ValueCard, id, PropagationOperation.Delete);
        await db.SaveChangesAsync(ct);
    }

    private async Task<ValueCard> LoadAsync(Guid id, CancellationToken ct) =>
        await db.ValueCards.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, ct)
        ?? throw new NotFoundException($"Value card {id} not found.");
}
