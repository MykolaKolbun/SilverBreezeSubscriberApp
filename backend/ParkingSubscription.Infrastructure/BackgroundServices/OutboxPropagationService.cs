using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ParkingSubscription.Application.Abstractions;
using ParkingSubscription.Domain.Enums;
using ParkingSubscription.Infrastructure.Persistence;

namespace ParkingSubscription.Infrastructure.BackgroundServices;

/// <summary>
/// Drains the outbox and asynchronously propagates state changes to Parking.Logic
/// with delivery tracking and bounded retries (ТЗ §5).
/// </summary>
public sealed class OutboxPropagationService(
    IServiceScopeFactory scopeFactory,
    ILogger<OutboxPropagationService> logger) : BackgroundService
{
    private const int MaxAttempts = 5;
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        do
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Outbox propagation batch failed");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task ProcessBatchAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var client = scope.ServiceProvider.GetRequiredService<IParkingLogicClient>();

        var pending = await db.OutboxMessages
            .Where(m => m.Status == OutboxStatus.Pending)
            .OrderBy(m => m.CreatedAt)
            .Take(50)
            .ToListAsync(ct);

        foreach (var message in pending)
        {
            try
            {
                await client.PropagateAsync(message.EntityKind, message.EntityId, message.Operation, message.PayloadJson, ct);
                message.Status = OutboxStatus.Delivered;
                message.DeliveredAt = DateTimeOffset.UtcNow;
                message.LastError = null;
            }
            catch (Exception ex)
            {
                message.Attempts++;
                message.LastError = ex.Message;
                if (message.Attempts >= MaxAttempts)
                    message.Status = OutboxStatus.Failed;
                logger.LogWarning(ex, "Failed to propagate outbox message {MessageId} (attempt {Attempts})",
                    message.Id, message.Attempts);
            }
            message.Touch();
        }

        if (pending.Count > 0)
            await db.SaveChangesAsync(ct);
    }
}
