using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using TaskBoard.Api.Data;
using TaskBoard.Api.Endpoints;

var builder = WebApplication.CreateBuilder(args);

// The connection string comes from configuration, not a hard-coded value —
// docker-compose.override.yml injects the real one (pointing at the "db"
// service by its container name) via an environment variable. Running
// outside Docker, appsettings.Development.json points at localhost instead.
var connectionString = builder.Configuration.GetConnectionString("TaskBoardDb")
    ?? throw new InvalidOperationException("Connection string 'TaskBoardDb' is not configured.");

builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// Create the schema on startup if it doesn't exist yet. This is the
// simplest option for a demo repo — no dotnet-ef tool, no Migrations/
// folder to keep in sync. A real service would use EF Core migrations
// (dotnet ef migrations add / database.Migrate()) instead; see the README.
//
// This retry loop isn't just insurance against a slow SQL Server first
// boot — it's load-bearing. The healthcheck in docker-compose.yml reports
// "healthy" as soon as SQL Server itself accepts connections, which is
// before EnsureCreatedAsync's very first call has actually created the
// TaskBoardDb database. That first attempt reliably fails (SQL Server only
// tells the client "Login failed for user 'sa'." either way, regardless of
// the real reason), and the retry is what gets far enough to create it.
await EnsureDatabaseReadyAsync(app.Services, app.Logger);

app.MapGet("/", () => "TaskBoard.Api is running. See README for endpoints.");

app.MapGet("/health", async (AppDbContext db) =>
    await db.Database.CanConnectAsync() ? Results.Ok("healthy") : Results.StatusCode(503));

app.MapTaskEndpoints();

app.Run();

static async Task EnsureDatabaseReadyAsync(IServiceProvider services, ILogger logger)
{
    using var scope = services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    const int maxAttempts = 10;
    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            await db.Database.EnsureCreatedAsync();
            logger.LogInformation("Database schema is ready.");
            return;
        }
        catch (Exception ex) when (attempt < maxAttempts)
        {
            logger.LogWarning("Database not ready yet (attempt {Attempt}/{MaxAttempts}): {Message}", attempt, maxAttempts, ex.Message);
            await Task.Delay(TimeSpan.FromSeconds(3));
        }
    }
}

public partial class Program { } // exposed for tests / WebApplicationFactory
