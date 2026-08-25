using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using ParkingSubscription.AdminPanel;
using ParkingSubscription.Application.Abstractions;
using ParkingSubscription.Infrastructure.Auth;
using ParkingSubscription.Infrastructure.Persistence;

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
