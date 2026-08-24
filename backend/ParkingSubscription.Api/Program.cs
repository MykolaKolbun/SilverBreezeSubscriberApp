using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ParkingSubscription.Api.Infrastructure;
using ParkingSubscription.Application;
using ParkingSubscription.Infrastructure;
using ParkingSubscription.Infrastructure.Auth;
using ParkingSubscription.Infrastructure.Persistence;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) => cfg.ReadFrom.Configuration(ctx.Configuration)
    .WriteTo.Console()
    .Enrich.FromLogContext());

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<AppExceptionHandler>();
builder.Services.AddOpenApi();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Data Protection — encrypts payment secrets at rest. Keys persist to a mounted
// volume (DataProtection:KeysPath) in production so restarts keep the same key;
// without a path (dev/tests) an ephemeral per-process key is used.
var dataProtection = builder.Services.AddDataProtection().SetApplicationName("SilverBreeze");
var keysPath = builder.Configuration["DataProtection:KeysPath"];
if (!string.IsNullOrWhiteSpace(keysPath))
{
    Directory.CreateDirectory(keysPath);
    dataProtection.PersistKeysToFileSystem(new DirectoryInfo(keysPath));
}

// Health checks: liveness + database connectivity (used by Docker/CI — ТЗ deployment).
builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>("database");

// JWT authentication (ТЗ §3)
var jwt = new JwtOptions();
builder.Configuration.GetSection(JwtOptions.SectionName).Bind(jwt);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });
builder.Services.AddAuthorization();

// Localization uk/en (ТЗ §9)
builder.Services.AddLocalization();
var supportedCultures = new[] { new CultureInfo("uk"), new CultureInfo("en") };

var app = builder.Build();

app.UseExceptionHandler();

app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("uk"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Health endpoint (anonymous) for container/CI health checks.
app.MapHealthChecks("/health").AllowAnonymous();

// Initialize the database and seed baseline data at startup.
await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // PostgreSQL (prod/Pi) applies committed migrations; the throwaway SQLite dev
    // database is created directly from the model (no migration set is kept for it).
    if (db.Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) == true)
        await db.Database.EnsureCreatedAsync();
    else
        await db.Database.MigrateAsync();

    await DbSeeder.SeedAsync(db);

    // Seed/refresh the iPay gateway config from environment (SignKey never lives in
    // appsettings — only in the Pi .env). The SignKey is encrypted before it is stored.
    var mchId = app.Configuration["Payment:iPay:MchId"];
    var signKey = app.Configuration["Payment:iPay:SignKey"];
    var ipayBaseUrl = app.Configuration["Payment:iPay:BaseUrl"];
    if (!string.IsNullOrWhiteSpace(mchId) && !string.IsNullOrWhiteSpace(signKey))
    {
        var protector = scope.ServiceProvider.GetRequiredService<ParkingSubscription.Application.Abstractions.ICredentialProtector>();
        var row = await db.PaymentGatewayConfigs.FindAsync([ParkingSubscription.Domain.Entities.PaymentGatewayConfig.SingletonId], cancellationToken: default)
                  ?? db.PaymentGatewayConfigs.Add(new ParkingSubscription.Domain.Entities.PaymentGatewayConfig()).Entity;
        row.MerchantId = mchId;
        row.SignKeyEncrypted = protector.Protect(signKey);
        row.BaseUrl = string.IsNullOrWhiteSpace(ipayBaseUrl) ? row.BaseUrl : ipayBaseUrl;
        row.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();
        Log.Information("iPay gateway config seeded from environment (merchant {MerchantId})", mchId);
    }
}

app.Run();

// Exposed for WebApplicationFactory in integration tests.
public partial class Program;
