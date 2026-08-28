using System.Net.Http.Headers;
using System.Text;
using Microsoft.EntityFrameworkCore;
using ParkingSubscription.Api.Subscribe;
using ParkingSubscription.Application.Abstractions;
using ParkingSubscription.Domain.Entities;
using ParkingSubscription.Domain.Enums;
using ParkingSubscription.Infrastructure.Persistence;
// Both namespaces define ParkingCard/ValueCard; here the bare names mean our entities.
using ParkingCard = ParkingSubscription.Domain.Entities.ParkingCard;
using ValueCard = ParkingSubscription.Domain.Entities.ValueCard;

namespace ParkingSubscription.Api.ParkingLogic;

/// <summary>
/// Real <see cref="IParkingLogicClient"/> backed by the SKIDATA sweb(R) Subscribe API
/// (the generated <see cref="Client"/>). Configuration lives in the single-row
/// <see cref="ParkingIntegrationConfig"/>, edited from the AdminPanel — no env secrets.
/// The row is read at the start of every propagation, so enabling/disabling or changing
/// credentials takes effect on the next outbox tick without a redeploy. When the config
/// is disabled or incomplete the client no-ops (logs and returns), exactly like the stub.
///
/// Drained by the outbox on the same scoped <see cref="AppDbContext"/> as the worker, so
/// the UUIDs sweb assigns on create are committed together with the delivery status.
/// </summary>
public sealed class SkidataParkingLogicClient(
    AppDbContext db,
    IHttpClientFactory httpFactory,
    ICredentialProtector protector,
    IClock clock,
    ILogger<SkidataParkingLogicClient> logger) : IParkingLogicClient
{
    // Set at the start of each PropagateAsync (scoped service, sequential outbox calls).
    private ParkingIntegrationConfig _cfg = default!;
    private Client _client = default!;

    public async Task<string> PropagateAsync(
        EntityKind kind, Guid entityId, PropagationOperation op, string? payloadJson, CancellationToken ct = default)
    {
        var cfg = await db.ParkingIntegrationConfigs.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == ParkingIntegrationConfig.SingletonId, ct);

        var username = Decrypt(cfg?.UsernameEncrypted);
        var password = Decrypt(cfg?.PasswordEncrypted);
        if (cfg is null || !cfg.Enabled
            || string.IsNullOrWhiteSpace(cfg.BaseUrl)
            || string.IsNullOrWhiteSpace(cfg.FacilityNumber)
            || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            logger.LogDebug("SKIDATA integration disabled/incomplete — skipping {Op} {Kind} {Id}", op, kind, entityId);
            return $"skidata:disabled:{kind}:{entityId:N}";
        }

        _cfg = cfg;
        _client = BuildClient(cfg, username, password);

        return kind switch
        {
            EntityKind.Customer => await HandleCustomerAsync(entityId, op, ct),
            EntityKind.User => await HandleUserAsync(entityId, op, ct),
            EntityKind.ParkingCard => await HandleParkingCardAsync(entityId, op, ct),
            EntityKind.ValueCard => await HandleValueCardAsync(entityId, op, ct),
            _ => throw new NotSupportedException($"Unknown entity kind {kind}")
        };
    }

    private Client BuildClient(ParkingIntegrationConfig cfg, string username, string password)
    {
        var http = httpFactory.CreateClient("skidata");
        var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", basic);
        return new Client(http) { BaseUrl = cfg.BaseUrl! };
    }

    private string? Decrypt(string? enc)
    {
        if (string.IsNullOrEmpty(enc)) return null;
        try { return protector.Unprotect(enc); }
        catch { return null; }
    }

    // ---- Customer ----------------------------------------------------------

    private async Task<string> HandleCustomerAsync(Guid id, PropagationOperation op, CancellationToken ct)
    {
        var customer = await db.Customers.FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new InvalidOperationException($"Customer {id} not found for propagation");

        var today = ToDate(clock.Today);
        switch (op)
        {
            case PropagationOperation.Create:
            case PropagationOperation.Update:
                if (customer.SkidataCustomerId is Guid updId)
                    await _client.UpdateCustomerAsync(updId, MapCustomer(customer), ct);
                else
                    await EnsureCustomerAsync(customer, ct);
                break;
            case PropagationOperation.Block:
                await _client.BlockCustomerAsync(await EnsureCustomerAsync(customer, ct), new BlockCustomer { StartDate = today }, ct);
                break;
            case PropagationOperation.Unblock:
                if (customer.SkidataCustomerId is Guid unId) await _client.UnblockCustomerAsync(unId, ct);
                break;
            case PropagationOperation.Anonymize:
                if (customer.SkidataCustomerId is Guid anId) await _client.AnonymizeCustomerAsync(anId, ct);
                break;
            case PropagationOperation.Delete:
            {
                // sweb requires deletion in reverse order: cards -> users -> customer.
                var facility = _cfg.FacilityNumber!;
                var users = await db.Users.Where(u => u.CustomerId == customer.Id).ToListAsync(ct);
                foreach (var u in users) await DeleteUserCascadeAsync(u, facility, ct);
                if (customer.SkidataCustomerId is Guid delId) await _client.DeleteCustomerAsync(delId, ct);
                break;
            }
            default:
                logger.LogWarning("Unsupported customer operation {Op}", op);
                break;
        }
        return RemoteRef(EntityKind.Customer, customer.SkidataCustomerId);
    }

    /// <summary>Returns the sweb customer UUID, creating the customer there if needed.</summary>
    private async Task<Guid> EnsureCustomerAsync(Customer customer, CancellationToken ct)
    {
        if (customer.SkidataCustomerId is Guid existing) return existing;

        var created = await _client.CreateCustomerAsync(customer.Id, MapCustomer(customer), ct);
        customer.SkidataCustomerId = created.CustomerId;
        customer.Touch();
        logger.LogInformation("SKIDATA customer created {LocalId} -> {RemoteId}", customer.Id, created.CustomerId);
        return created.CustomerId;
    }

    private CreateCustomer MapCustomer(Customer c) => new()
    {
        Surname = NonEmpty(c.Surname ?? c.Name, "Customer"),
        Firstname = c.FirstName,
        Email = c.Email,
        ExternalContactId = c.ExternalContactId ?? c.Id.ToString("N")
    };

    // ---- User --------------------------------------------------------------

    private async Task<string> HandleUserAsync(Guid id, PropagationOperation op, CancellationToken ct)
    {
        var user = await db.Users.Include(u => u.Customer).FirstOrDefaultAsync(u => u.Id == id, ct)
            ?? throw new InvalidOperationException($"User {id} not found for propagation");

        var today = ToDate(clock.Today);
        switch (op)
        {
            case PropagationOperation.Create:
            case PropagationOperation.Update:
                if (user.SkidataUserId is Guid updId)
                    await _client.UpdateUserAsync(updId, await MapUserAsync(user, ct), ct);
                else
                    await EnsureUserAsync(user, ct);
                break;
            case PropagationOperation.Block:
                await _client.BlockUserAsync(await EnsureUserAsync(user, ct), new BlockUser { StartDate = today }, ct);
                break;
            case PropagationOperation.Unblock:
                if (user.SkidataUserId is Guid unId) await _client.UnblockUserAsync(unId, ct);
                break;
            case PropagationOperation.Suspend:
                await _client.SuspendUserAsync(await EnsureUserAsync(user, ct),
                    new SuspendUser { StartDate = today, EndDate = today }, ct);
                break;
            case PropagationOperation.Resume:
                if (user.SkidataUserId is Guid reId) await _client.ResumeUserAsync(reId, ct);
                break;
            case PropagationOperation.Anonymize:
                if (user.SkidataUserId is Guid anId) await _client.AnonymizeUserAsync(anId, ct);
                break;
            case PropagationOperation.Delete:
                // Reverse order: delete the user's cards before the user itself.
                await DeleteUserCascadeAsync(user, _cfg.FacilityNumber!, ct);
                break;
            default:
                logger.LogWarning("Unsupported user operation {Op}", op);
                break;
        }
        return RemoteRef(EntityKind.User, user.SkidataUserId);
    }

    /// <summary>Returns the sweb user UUID, creating the user (and its customer) there if needed.</summary>
    private async Task<Guid> EnsureUserAsync(User user, CancellationToken ct)
    {
        if (user.SkidataUserId is Guid existing) return existing;

        var created = await _client.CreateUserAsync(user.Id, await MapUserAsync(user, ct), ct);
        user.SkidataUserId = created.UserId;
        user.Touch();
        logger.LogInformation("SKIDATA user created {LocalId} -> {RemoteId}", user.Id, created.UserId);
        return created.UserId;
    }

    private async Task<CreateUser> MapUserAsync(User user, CancellationToken ct)
    {
        var customer = user.Customer ?? await db.Customers.FirstOrDefaultAsync(c => c.Id == user.CustomerId, ct)
            ?? throw new InvalidOperationException($"Customer {user.CustomerId} of user {user.Id} not found");
        var customerRemoteId = await EnsureCustomerAsync(customer, ct);

        var dto = new CreateUser
        {
            Surname = NonEmpty(user.Surname ?? user.Name, "Parker"),
            Firstname = user.FirstName,
            Email = user.Email,
            Mobile = user.Mobile,
            ExternalContactId = user.ExternalContactId ?? user.Id.ToString("N"),
            // License-plate entry policy lives under user.parkingContract in sweb.
            ParkingContract = new UserParkingContract
            {
                PassageLP = user.PassageLp,
                CheckLP = user.CheckLp,
                MatchEntryPlate = user.MatchEntryPlate
            }
        };
        if (string.Equals(_cfg.CustomerLinkField, "group", StringComparison.OrdinalIgnoreCase))
            dto.GroupCustomerId = customerRemoteId;
        else
            dto.B2bCustomerId = customerRemoteId;

        // Vehicles -> sweb licensePlates[] (country + plate + make/model description).
        var vehicles = await db.Vehicles.AsNoTracking()
            .Where(v => v.UserId == user.Id && !v.IsDeleted).ToListAsync(ct);
        if (vehicles.Count > 0)
            dto.LicensePlates = vehicles.Select(v => new LicensePlate
            {
                Country = NonEmpty(v.Country, "UA"),
                Value = v.PlateNumber,
                Vehicle = NullIfEmpty($"{v.Make} {v.Model}")
            }).ToList();

        return dto;
    }

    // ---- Parking card ------------------------------------------------------

    private async Task<string> HandleParkingCardAsync(Guid id, PropagationOperation op, CancellationToken ct)
    {
        var facility = _cfg.FacilityNumber!;
        var card = await db.ParkingCards.Include(c => c.User).ThenInclude(u => u!.Customer)
            .FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new InvalidOperationException($"ParkingCard {id} not found for propagation");

        var today = ToDate(clock.Today);
        switch (op)
        {
            case PropagationOperation.Create:
                if (card.SkidataCardId is null) await EnsureParkingCardAsync(facility, card, ct);
                break;
            case PropagationOperation.Update:
                if (card.SkidataCardId is Guid updId)
                    await _client.UpdateParkingCardAsync(facility, updId, new UpdateParkingCard
                    {
                        ValidFrom = ToDate(card.StartDate),
                        ValidTo = ToDate(card.EndDate),
                        SingleNeutral = card.SingleNeutral,
                        PrimaryId = QrIdentification(card),
                        SecondaryIds = MapSecondaryIds(card),
                        CarParks = MapCarParks(card)
                    }, ct);
                else
                    await EnsureParkingCardAsync(facility, card, ct);
                break;
            case PropagationOperation.Block:
                await _client.BlockParkingCardAsync(facility, await EnsureParkingCardAsync(facility, card, ct),
                    new BlockParkingCard { StartDate = today }, ct);
                break;
            case PropagationOperation.Unblock:
                if (card.SkidataCardId is Guid unId) await _client.UnblockParkingCardAsync(facility, unId, ct);
                break;
            case PropagationOperation.Suspend:
                await _client.SuspendParkingCardAsync(facility, await EnsureParkingCardAsync(facility, card, ct),
                    new SuspendParkingCard { StartDate = ToDate(card.StartDate), EndDate = ToDate(card.EndDate) }, ct);
                break;
            case PropagationOperation.Resume:
                if (card.SkidataCardId is Guid reId) await _client.ResumeParkingCardAsync(facility, reId, ct);
                break;
            case PropagationOperation.Anonymize:
                if (card.SkidataCardId is Guid anId) await _client.AnonymizeParkingCardAsync(facility, anId, ct);
                break;
            case PropagationOperation.Delete:
                if (card.SkidataCardId is Guid delId) await _client.DeleteParkingCardAsync(facility, delId, ct);
                break;
            default:
                logger.LogWarning("Unsupported parking card operation {Op}", op);
                break;
        }
        return RemoteRef(EntityKind.ParkingCard, card.SkidataCardId);
    }

    private async Task<Guid> EnsureParkingCardAsync(string facility, ParkingCard card, CancellationToken ct)
    {
        if (card.SkidataCardId is Guid existing) return existing;

        var user = card.User ?? await db.Users.Include(u => u.Customer).FirstOrDefaultAsync(u => u.Id == card.UserId, ct)
            ?? throw new InvalidOperationException($"User {card.UserId} of card {card.Id} not found");
        var userRemoteId = await EnsureUserAsync(user, ct);

        var body = new CreateParkingCard
        {
            UserId = userRemoteId,
            ValidFrom = ToDate(card.StartDate),
            ValidTo = ToDate(card.EndDate),
            SingleNeutral = card.SingleNeutral,
            ExternalCardId = card.ExternalCardId ?? card.Id.ToString("N"),
            PrimaryId = QrIdentification(card),
            SecondaryIds = MapSecondaryIds(card),
            CarParks = MapCarParks(card)
        };
        var productId = await ResolveProductIdAsync(card, ct);
        if (productId is Guid pid) body.ProductId = pid;

        var created = await _client.CreateParkingCardAsync(facility, card.Id, body, ct);
        card.SkidataCardId = created.ParkingCardId;
        if (productId is Guid p) card.ProductId = p;
        card.Touch();
        logger.LogInformation("SKIDATA parking card created {LocalId} -> {RemoteId}", card.Id, created.ParkingCardId);
        return created.ParkingCardId;
    }

    /// <summary>
    /// The sweb productId for a card comes from its plan's <see cref="SubscriptionPlan.ArticleId"/>
    /// (a UUID). Falls back to a productId already stored on the card. Null → the field is omitted.
    /// </summary>
    private async Task<Guid?> ResolveProductIdAsync(ParkingCard card, CancellationToken ct)
    {
        if (card.SubscriptionPlanId is Guid planId)
        {
            var articleId = await db.SubscriptionPlans.AsNoTracking()
                .Where(p => p.Id == planId).Select(p => p.ArticleId).FirstOrDefaultAsync(ct);
            if (Guid.TryParse(articleId, out var fromPlan)) return fromPlan;
            if (!string.IsNullOrWhiteSpace(articleId))
                logger.LogWarning("Plan {PlanId} ArticleId '{ArticleId}' is not a valid sweb productId (GUID)", planId, articleId);
        }
        if (card.ProductId is Guid stored) return stored;
        logger.LogWarning("No sweb productId (ArticleId) for card {CardId} — creating card without productId", card.Id);
        return null;
    }

    private Identification QrIdentification(ParkingCard card) => new()
    {
        Type = NonEmpty(_cfg.QrIdentificationType, "EXT"),
        SubType = NonEmpty(_cfg.QrIdentificationSubType, "_SDCP"),
        Value = card.QrPayload
    };

    /// <summary>Secondary card identifications (sweb secondaryIds); null when none.</summary>
    private static ICollection<Identification>? MapSecondaryIds(ParkingCard card) =>
        card.SecondaryIds.Count == 0
            ? null
            : card.SecondaryIds.Select(s => new Identification
            {
                Type = s.Type,
                SubType = s.SubType,
                Value = s.Value
            }).ToList();

    /// <summary>Car parks the card is valid for (sweb carParks); null when none.</summary>
    private static ICollection<CarPark>? MapCarParks(ParkingCard card) =>
        card.CarParks.Count == 0
            ? null
            : card.CarParks.Select(c => new CarPark
            {
                CarParkNumber = c.CarParkNumber,
                EntryType = ToSwebEntryType(c.EntryType)
            }).ToList();

    private static EntryType ToSwebEntryType(CarParkEntryType e) => e switch
    {
        CarParkEntryType.AccessGrantedWithTimeWindow => EntryType.ACCESS_GRANTED_WITH_TIMEWINDOW,
        CarParkEntryType.AccessGrantedNoTimeWindow => EntryType.ACCESS_GRANTED_NO_TIMEWINDOW,
        CarParkEntryType.AccessGrantedWithTimeWindowCarParkFull => EntryType.ACCESS_GRANTED_WITH_TIMEWINDOW_CARPARK_FULL,
        _ => EntryType.ACCESS_NOT_GRANTED
    };

    // ---- Value card --------------------------------------------------------

    private async Task<string> HandleValueCardAsync(Guid id, PropagationOperation op, CancellationToken ct)
    {
        var facility = _cfg.FacilityNumber!;
        var card = await db.ValueCards.Include(c => c.User).ThenInclude(u => u!.Customer)
            .FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new InvalidOperationException($"ValueCard {id} not found for propagation");

        var today = ToDate(clock.Today);
        switch (op)
        {
            case PropagationOperation.Create:
            case PropagationOperation.Update:
                if (card.SkidataCardId is null) await EnsureValueCardAsync(facility, card, ct);
                break;
            case PropagationOperation.Block:
                await _client.BlockValueCardAsync(facility, await EnsureValueCardAsync(facility, card, ct),
                    new BlockValueCard { StartDate = today }, ct);
                break;
            case PropagationOperation.Unblock:
                if (card.SkidataCardId is Guid unId) await _client.UnblockValueCardAsync(facility, unId, ct);
                break;
            case PropagationOperation.Anonymize:
                if (card.SkidataCardId is Guid anId) await _client.AnonymizeValueCardAsync(facility, anId, ct);
                break;
            case PropagationOperation.Delete:
                if (card.SkidataCardId is Guid delId) await _client.DeleteValueCardAsync(facility, delId, ct);
                break;
            default:
                logger.LogWarning("Unsupported value card operation {Op}", op);
                break;
        }
        return RemoteRef(EntityKind.ValueCard, card.SkidataCardId);
    }

    private async Task<Guid> EnsureValueCardAsync(string facility, ValueCard card, CancellationToken ct)
    {
        if (card.SkidataCardId is Guid existing) return existing;

        var user = card.User ?? await db.Users.Include(u => u.Customer).FirstOrDefaultAsync(u => u.Id == card.UserId, ct)
            ?? throw new InvalidOperationException($"User {card.UserId} of value card {card.Id} not found");
        var userRemoteId = await EnsureUserAsync(user, ct);

        var body = new CreateValueCard
        {
            UserId = userRemoteId,
            ExternalCardId = card.ExternalCardId ?? card.Id.ToString("N")
        };
        if (_cfg.ValueProductId is Guid pid) body.ProductId = pid;

        var created = await _client.CreateValueCardAsync(facility, card.Id, body, ct);
        card.SkidataCardId = created.ValueCardId;
        card.Touch();
        logger.LogInformation("SKIDATA value card created {LocalId} -> {RemoteId}", card.Id, created.ValueCardId);
        return created.ValueCardId;
    }

    // ---- Cascade delete (reverse order: cards -> user -> customer) ----------

    /// <summary>Deletes a user's cards in sweb, then the user itself.</summary>
    private async Task DeleteUserCascadeAsync(User user, string facility, CancellationToken ct)
    {
        var parkingCards = await db.ParkingCards
            .Where(c => c.UserId == user.Id && c.SkidataCardId != null).ToListAsync(ct);
        foreach (var c in parkingCards)
            if (c.SkidataCardId is Guid pcid) await _client.DeleteParkingCardAsync(facility, pcid, ct);

        var valueCards = await db.ValueCards
            .Where(c => c.UserId == user.Id && c.SkidataCardId != null).ToListAsync(ct);
        foreach (var c in valueCards)
            if (c.SkidataCardId is Guid vcid) await _client.DeleteValueCardAsync(facility, vcid, ct);

        if (user.SkidataUserId is Guid uid) await _client.DeleteUserAsync(uid, ct);
    }

    // ---- Helpers -----------------------------------------------------------

    private static DateTimeOffset ToDate(DateOnly d) =>
        new(d.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

    private static string NonEmpty(string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string RemoteRef(EntityKind kind, Guid? remoteId) =>
        remoteId is Guid g ? $"skidata:{kind}:{g:N}" : $"skidata:{kind}:pending";
}
