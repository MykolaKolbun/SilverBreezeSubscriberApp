using Microsoft.Extensions.Logging;
using ParkingSubscription.Application.Abstractions;
using ParkingSubscription.Domain.Enums;

namespace ParkingSubscription.Infrastructure.ParkingLogic;

/// <summary>
/// Stub for the future Parking.Logic external API (ТЗ §4). Logs the propagation
/// and returns a synthetic external id. Replace with a real HTTP client once the
/// external specification is available (ТЗ §10.6).
/// </summary>
public sealed class ParkingLogicClientStub(ILogger<ParkingLogicClientStub> logger) : IParkingLogicClient
{
    public Task<string> PropagateAsync(EntityKind kind, Guid entityId, PropagationOperation op, string? payloadJson, CancellationToken ct = default)
    {
        var externalId = $"PL-{kind}-{entityId:N}";
        logger.LogInformation("Parking.Logic propagate: {Operation} {Kind} {EntityId} -> {ExternalId}",
            op, kind, entityId, externalId);
        return Task.FromResult(externalId);
    }
}
