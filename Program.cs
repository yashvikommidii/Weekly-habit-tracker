using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using WeeklyHabitTracker.Data;
using WeeklyHabitTracker.Services;

var builder = WebApplication.CreateBuilder(args);

// Use PORT from environment (e.g. Render, Heroku)
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port))
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// Add controllers
builder.Services.AddControllers();
builder.Services.AddSingleton<HabitStorage>();
builder.Services.AddHttpClient();

// SQL Server / Entity Framework
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

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

// Enable Swagger in development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Weekly Habit Tracker API v1");
    });
}

// Serve static files from wwwroot (index.html, CSS, JS)
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseRouting();
app.MapControllers();
app.MapFallbackToFile("index.html");

// Ensure database is created, migrated, and seeded
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    await DbSeeder.SeedAsync(db);
}

app.Run();
