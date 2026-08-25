using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ParkingSubscription.Domain.Enums;
using ParkingSubscription.Infrastructure.Persistence;

namespace ParkingSubscription.AdminPanel.Pages;

public sealed class ClientsModel(AppDbContext db, ILogger<ClientsModel> logger) : PageModel
{
    public IReadOnlyList<ClientRow> Clients { get; private set; } = [];
    public string? LoadError { get; private set; }

    public async Task OnGetAsync(CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        try
        {
            Clients = await db.Users
                .AsNoTracking()
                .OrderBy(u => u.CreatedAt)
                .Select(u => new ClientRow
                {
                    Id = u.Id,
                    Name = ((u.FirstName ?? "") + " " + (u.Surname ?? "")).Trim(),
                    Email = db.AppAccounts.Where(a => a.UserId == u.Id).Select(a => a.Email).FirstOrDefault()
                            ?? u.Email,
                    Phone = db.AppAccounts.Where(a => a.UserId == u.Id).Select(a => a.Phone).FirstOrDefault(),
                    HasActiveSubscription = db.ParkingCards.Any(c =>
                        c.UserId == u.Id && c.Status == CardStatus.Active && !c.IsDeleted && c.EndDate >= today),
                })
                .ToListAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load clients");
            LoadError = "Не вдалося завантажити дані з бази.";
        }
    }

    public sealed class ClientRow
    {
        public Guid Id { get; init; }
        public string? Name { get; init; }
        public string? Email { get; init; }
        public string? Phone { get; init; }
        public bool HasActiveSubscription { get; init; }
    }
}
