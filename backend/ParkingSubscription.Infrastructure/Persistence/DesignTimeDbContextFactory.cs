using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ParkingSubscription.Infrastructure.Persistence;

/// <summary>
/// Design-time factory used by <c>dotnet ef migrations</c>. Migrations target
/// PostgreSQL (the production/Pi database); local SQLite dev uses
/// <c>EnsureCreated</c> instead of migrations, so only one migration set is kept.
/// Generation is offline — no live database connection is required.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Default")
            ?? "Host=localhost;Port=5432;Database=parking;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new AppDbContext(options);
    }
}
