using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using WeeklyHabitTracker.Data;
using WeeklyHabitTracker.Services;

var builder = WebApplication.CreateBuilder(args);

// Use PORT from environment (e.g. Render, Heroku)
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}
else if (builder.Environment.EnvironmentName == "Development")
{
    // Local dev: listen on both HTTP and HTTPS
    builder.WebHost.UseUrls("http://localhost:5080", "https://localhost:5081");
}

// HTTPS redirection (redirect HTTP to HTTPS)
builder.Services.AddHttpsRedirection(options =>
{
    options.RedirectStatusCode = 307;
    options.HttpsPort = string.IsNullOrEmpty(port) ? 5081 : null; // 5081 for local dev, null for cloud (443)
});
builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(365);
    options.IncludeSubDomains = true;
});
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
});

// Rate limiting
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("chat", config =>
    {
        config.Window = TimeSpan.FromMinutes(1);
        config.PermitLimit = 20;
    });
    options.AddFixedWindowLimiter("api", config =>
    {
        config.Window = TimeSpan.FromMinutes(1);
        config.PermitLimit = 120;
    });
    options.RejectionStatusCode = 429;
    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.Headers["Retry-After"] = "60";
        await ValueTask.CompletedTask;
    };
});

// Add controllers
builder.Services.AddControllers();
builder.Services.AddSingleton<HabitStorage>();
builder.Services.AddSingleton<RateLimitService>();
builder.Services.AddHttpClient();

// SQL Server / Entity Framework (fallback to in-memory when connection string is missing/invalid)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var useSqlServer = !string.IsNullOrWhiteSpace(connectionString) &&
    (connectionString.StartsWith("Server=", StringComparison.OrdinalIgnoreCase) ||
     connectionString.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase));

if (useSqlServer)
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(connectionString));
}
else
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseInMemoryDatabase("HabitTrackerDb"));
}

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Weekly Habit Tracker API",
        Version = "v1",
        Description = "API for tracking habits throughout the week and generating progress graphs"
    });
});

var app = builder.Build();

// Enable Swagger in development (disable via DisableSwagger config or DISABLE_SWAGGER env for security scans)
var disableSwagger = app.Configuration.GetValue<bool>("DisableSwagger") ||
    string.Equals(Environment.GetEnvironmentVariable("DISABLE_SWAGGER"), "true", StringComparison.OrdinalIgnoreCase);
if (app.Environment.IsDevelopment() && !disableSwagger)
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Weekly Habit Tracker API v1");
    });
}

// Forwarded headers, HSTS, HTTPS redirect + Security headers + rate limit headers
app.UseForwardedHeaders();
app.UseHsts();
app.UseHttpsRedirection();

app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";
    context.Response.Headers["Cross-Origin-Opener-Policy"] = "same-origin";
    context.Response.Headers["X-DNS-Prefetch-Control"] = "off";
    context.Response.Headers["X-RateLimit-Limit"] = "120";
    context.Response.Headers["X-RateLimit-Policy"] = "120;w=60";
    context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; font-src 'self' https://fonts.gstatic.com; img-src 'self' data:; connect-src 'self' https://api.openai.com;";
    await next();
});

// Simple rate limit for homepage (static files bypass controller rate limiting)
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value ?? "";
    if (path == "/" || path.Equals("/index.html", StringComparison.OrdinalIgnoreCase))
    {
        var limiter = context.RequestServices.GetRequiredService<RateLimitService>();
        if (!limiter.TryAcquire(context.Connection.RemoteIpAddress?.ToString() ?? "unknown"))
        {
            context.Response.StatusCode = 429;
            return;
        }
    }
    await next();
});

// Serve static files from wwwroot (index.html, CSS, JS)
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseRouting();
app.UseRateLimiter();
app.MapControllers();
// Return 404 for unknown routes (reduces Attack Agent false positives)
// - Unknown /api/* paths (scanner probes /api/vulnerable, /api/demo, etc.)
// - Unknown root paths (scanner probes /logout, /signin, /auth, /.git/config, etc.)
// Valid app entry: / and /index.html are served by UseDefaultFiles + UseStaticFiles
app.MapFallback((HttpContext ctx) => { ctx.Response.StatusCode = 404; return Task.CompletedTask; });

// Ensure database is created, migrated, and seeded
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (useSqlServer)
    {
        db.Database.Migrate();
    }
    else
    {
        await db.Database.EnsureCreatedAsync();
    }
    await DbSeeder.SeedAsync(db);
}

app.Run();
