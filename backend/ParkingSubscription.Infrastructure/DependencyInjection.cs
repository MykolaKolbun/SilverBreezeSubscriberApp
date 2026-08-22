using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ParkingSubscription.Application.Abstractions;
using ParkingSubscription.Application.Auth;
using ParkingSubscription.Infrastructure.Auth;
using ParkingSubscription.Infrastructure.BackgroundServices;
using ParkingSubscription.Infrastructure.Notifications;
using ParkingSubscription.Infrastructure.ParkingLogic;
using ParkingSubscription.Infrastructure.Payments;
using ParkingSubscription.Infrastructure.Persistence;
using ParkingSubscription.Infrastructure.Time;
using ParkingSubscription.Infrastructure.Wallet;

namespace ParkingSubscription.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers infrastructure: EF Core DbContext (SQLite dev / PostgreSQL prod),
    /// external-integration stubs, auth primitives and background workers.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var provider = config["Database:Provider"] ?? "Sqlite";
        var connectionString = config.GetConnectionString("Default")
            ?? "Data Source=parking.db";

        services.AddDbContext<AppDbContext>(options =>
        {
            if (provider.Equals("Postgres", StringComparison.OrdinalIgnoreCase))
                options.UseNpgsql(connectionString);
            else
                options.UseSqlite(connectionString);
        });
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        // Options
        services.Configure<JwtOptions>(config.GetSection(JwtOptions.SectionName));
        var authOptions = new AuthOptions();
        config.GetSection("Auth").Bind(authOptions);
        services.AddSingleton(authOptions);

        // Auth primitives
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();

        // External-integration stubs (ТЗ §4, §6, §7, §9)
        services.AddScoped<IParkingLogicClient, ParkingLogicClientStub>();
        services.AddScoped<IPaymentProvider, PaymentProviderStub>();
        services.AddScoped<IFiscalProvider, FiscalProviderStub>();
        services.AddScoped<IWalletPassService, WalletPassServiceStub>();
        services.AddSingleton<IQrCodeGenerator, QrCodeGenerator>();
        services.AddScoped<IEmailSender, LoggingEmailSender>();
        services.AddScoped<IPushSender, LoggingPushSender>();

        // Background workers (ТЗ §5)
        services.AddHostedService<OutboxPropagationService>();
        services.AddHostedService<AnonymizationWorker>();

        return services;
    }
}
