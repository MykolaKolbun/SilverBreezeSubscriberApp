using ParkingSubscription.Web;

var builder = WebApplication.CreateBuilder(args);

var razorPages = builder.Services.AddRazorPages();
// Live-edit .cshtml without restarting the server (dev only).
if (builder.Environment.IsDevelopment())
    razorPages.AddRazorRuntimeCompilation();

// Server-side session keeps the JWT out of the browser entirely.
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(o =>
{
    o.Cookie.HttpOnly = true;
    o.Cookie.IsEssential = true;
    o.IdleTimeout = TimeSpan.FromHours(8);
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient<ApiClient>(http =>
    http.BaseAddress = new Uri(builder.Configuration["Api:BaseUrl"] ?? "http://localhost:5024"));

var app = builder.Build();

app.UseStaticFiles();
app.UseSession();
app.MapRazorPages();

app.Run();
