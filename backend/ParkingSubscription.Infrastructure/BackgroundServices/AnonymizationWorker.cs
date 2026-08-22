using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ParkingSubscription.Domain.Enums;
using ParkingSubscription.Infrastructure.Persistence;

namespace ParkingSubscription.Infrastructure.BackgroundServices;

/// <summary>
/// Performs the deferred anonymization: overwrites contact data of entities
/// marked <see cref="AnonymizationState.ReadyForAnonymization"/> with random
/// values and marks them anonymized (ТЗ §5, §9 GDPR-like).
/// </summary>
public sealed class AnonymizationWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<AnonymizationWorker> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(15);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        do
        {
            try
            {
                await ProcessAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Anonymization batch failed");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task ProcessAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var users = await db.Users
            .Where(u => u.AnonymizationState == AnonymizationState.ReadyForAnonymization)
            .Take(50)
            .ToListAsync(ct);

        foreach (var user in users)
        {
            var rnd = Guid.NewGuid().ToString("N")[..12];
            user.Name = $"anon-{rnd}";
            user.Surname = "anon";
            user.FirstName = "anon";
            user.Email = $"anon-{rnd}@anonymized.invalid";
            user.ExternalContactId = null;
            user.AnonymizationState = AnonymizationState.Anonymized;
            user.Touch();
        }

        var cards = await db.ParkingCards
            .Where(c => c.AnonymizationState == AnonymizationState.ReadyForAnonymization)
            .Take(50)
            .ToListAsync(ct);

        foreach (var card in cards)
        {
            card.ExternalCardId = null;
            card.AnonymizationState = AnonymizationState.Anonymized;
            card.Touch();
        }

        if (users.Count > 0 || cards.Count > 0)
        {
            await db.SaveChangesAsync(ct);
            logger.LogInformation("Anonymized {Users} users and {Cards} cards", users.Count, cards.Count);
        }
    }
}
