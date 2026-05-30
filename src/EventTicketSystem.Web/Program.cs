using EventTicketSystem.Web.Data;
using EventTicketSystem.Web.Models;
using EventTicketSystem.Web.Models.Configuration;
using EventTicketSystem.Web.Services;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

// Dynamic port: Cloud Run sets PORT, Azure Container Apps uses 8080 by default
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls($"http://+:{port}");

builder.Services.AddRazorPages();
builder.Services.AddControllers();

// Connection string priority: env var ConnectionStrings__DefaultConnection > appsettings.json
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit           = true;
    options.Password.RequiredLength         = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase       = false;
    options.SignIn.RequireConfirmedAccount  = false;
    options.Lockout.AllowedForNewUsers      = true;
    options.Lockout.DefaultLockoutTimeSpan  = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 10;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath        = "/Account/Login";
    options.LogoutPath       = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan   = TimeSpan.FromDays(7);
});

// AI secrets: env vars AI__ApiKey / AI__Model / AI__Endpoint override appsettings.json
builder.Services.Configure<AISettings>(builder.Configuration.GetSection("AI"));
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient("groq");
builder.Services.AddSingleton<TicketPredictionService>();
builder.Services.AddScoped<EventTicketSystem.Web.Services.CartService>();
builder.Services.AddScoped<EventTicketSystem.Web.Services.EmailService>();
builder.Services.AddScoped<EventTicketSystem.Web.Services.GroqService>();
builder.Services.AddScoped<EventTicketSystem.Web.Services.RefundService>();

// Password reset token valid for 1 hour
builder.Services.Configure<Microsoft.AspNetCore.Identity.DataProtectionTokenProviderOptions>(o =>
    o.TokenLifespan = TimeSpan.FromHours(1));

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout        = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly    = true;
    options.Cookie.IsEssential = true;
});

// Trust X-Forwarded-* headers from cloud load balancers (GCR, Azure Front Door, etc.)
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

// Auto-migrate on startup — non-fatal: app keeps running even if DB is unavailable
using (var scope = app.Services.CreateScope())
{
    var startupLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var db    = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        db.Database.Migrate();
        await DbSeeder.SeedAsync(db, roles, users);
        startupLogger.LogInformation("Database migration and seeding completed.");
    }
    catch (Exception ex)
    {
        startupLogger.LogError(ex, "Database migration/seeding failed — app continues without full DB init.");
    }
}

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// In containers (PORT is set), TLS is terminated at the cloud load balancer — skip redirect
var runningInContainer = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PORT"));
if (!runningInContainer)
    app.UseHttpsRedirection();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();
app.MapControllers();

var aiKey = app.Configuration["AI:ApiKey"];
if (string.IsNullOrWhiteSpace(aiKey))
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
    Console.WriteLine("║  AI chat disabled — set env var AI__ApiKey (Groq free key)  ║");
    Console.WriteLine("║  Get it at: https://console.groq.com                        ║");
    Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
    Console.ResetColor();
}

app.Run();
