using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using ParkingSubscription.AdminPanel;
using ParkingSubscription.Application.Abstractions;
using ParkingSubscription.Infrastructure.Auth;
using ParkingSubscription.Infrastructure.Persistence;
using ParkingSubscription.Infrastructure.Security;

var builder = WebApplication.CreateBuilder(args);

// Read-only view over the same database the API owns (no migrations applied here).
var provider = builder.Configuration["Database:Provider"] ?? "Sqlite";
var connectionString = builder.Configuration.GetConnectionString("Default") ?? "Data Source=parking.db";
builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (provider.Equals("Postgres", StringComparison.OrdinalIgnoreCase))
        options.UseNpgsql(connectionString);
    else
        options.UseSqlite(connectionString);
});
builder.Services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());
builder.Services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
builder.Services.AddScoped<AdminPasswordStore>();

// Share the API's Data Protection keys so secrets we encrypt here (iPay SignKey,
// Checkbox PIN/License) can be decrypted by the API — same keys + application name.
var dp = builder.Services.AddDataProtection().SetApplicationName("SilverBreeze");
var keysPath = builder.Configuration["DataProtection:KeysPath"];
if (!string.IsNullOrWhiteSpace(keysPath))
{
    Directory.CreateDirectory(keysPath);
    dp.PersistKeysToFileSystem(new DirectoryInfo(keysPath));
}
builder.Services.AddScoped<ICredentialProtector, CredentialProtector>();

// Cookie auth gated by a single admin password (Admin:Password / Admin__Password).
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/Login";
        o.AccessDeniedPath = "/Login";
        o.ExpireTimeSpan = TimeSpan.FromHours(8);
        o.Cookie.HttpOnly = true;
        o.Cookie.IsEssential = true;
    });
builder.Services.AddAuthorization();

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/");
    options.Conventions.AllowAnonymousToPage("/Login");
});

var app = builder.Build();

app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();

app.Run();
