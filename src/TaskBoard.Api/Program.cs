using Microsoft.EntityFrameworkCore;
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

var app = builder.Build();

// Create the schema on startup if it doesn't exist yet. This is the
// simplest option for a demo repo — no dotnet-ef tool, no Migrations/
// folder to keep in sync. A real service would use EF Core migrations
// (dotnet ef migrations add / database.Migrate()) instead; see the README.
//
// SQL Server takes a few seconds to accept connections after its
// container starts. docker-compose.yml gives it a healthcheck and the API
// service waits on it (depends_on: condition: service_healthy), but this
// retry loop is cheap insurance against a slow first boot either way.
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
