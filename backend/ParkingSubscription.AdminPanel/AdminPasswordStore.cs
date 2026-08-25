using Microsoft.EntityFrameworkCore;
using ParkingSubscription.Application.Abstractions;
using ParkingSubscription.Domain.Entities;
using ParkingSubscription.Infrastructure.Persistence;

namespace ParkingSubscription.AdminPanel;

/// <summary>
/// The admin login password, stored HASHED in the DB so it can be changed from the panel.
/// Seeded from the Admin:Password config (default "Passw0rd") on first use; after that the
/// DB value is authoritative.
/// </summary>
public sealed class AdminPasswordStore(AppDbContext db, IPasswordHasher hasher, IConfiguration config)
{
    public async Task<bool> VerifyAsync(string password, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(password)) return false;
        var cfg = await EnsureAsync(ct);
        return hasher.Verify(password, cfg.PasswordHash);
    }

    public async Task ChangeAsync(string newPassword, CancellationToken ct = default)
    {
        var cfg = await EnsureAsync(ct);
        cfg.PasswordHash = hasher.Hash(newPassword);
        cfg.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    private async Task<AdminConfig> EnsureAsync(CancellationToken ct)
    {
        var cfg = await db.AdminConfigs.FirstOrDefaultAsync(x => x.Id == AdminConfig.SingletonId, ct);
        if (cfg is null)
        {
            var seed = config["Admin:Password"] ?? "Passw0rd";
            cfg = new AdminConfig { PasswordHash = hasher.Hash(seed), UpdatedAt = DateTimeOffset.UtcNow };
            db.AdminConfigs.Add(cfg);
            await db.SaveChangesAsync(ct);
        }
        return cfg;
    }
}
