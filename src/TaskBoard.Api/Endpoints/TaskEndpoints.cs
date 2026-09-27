using Microsoft.EntityFrameworkCore;
using TaskBoard.Api.Data;
using TaskBoard.Api.Models;

namespace TaskBoard.Api.Endpoints;

public sealed record CreateTaskRequest(string Title);

public static class TaskEndpoints
{
    public static IEndpointRouteBuilder MapTaskEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/tasks");

        group.MapGet("/", async (AppDbContext db) =>
            await db.Tasks.OrderBy(t => t.CreatedAtUtc).ToListAsync());

        group.MapGet("/{id:int}", async (int id, AppDbContext db) =>
            await db.Tasks.FindAsync(id) is { } task ? Results.Ok(task) : Results.NotFound());

        group.MapPost("/", async (CreateTaskRequest req, AppDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(req.Title))
            {
                return Results.BadRequest("Title is required.");
            }

            var task = new TaskItem { Title = req.Title };
            db.Tasks.Add(task);
            await db.SaveChangesAsync();
            return Results.Created($"/tasks/{task.Id}", task);
        });

        group.MapPost("/{id:int}/complete", async (int id, AppDbContext db) =>
        {
            var task = await db.Tasks.FindAsync(id);
            if (task is null)
            {
                return Results.NotFound();
            }

            task.IsComplete = true;
            await db.SaveChangesAsync();
            return Results.Ok(task);
        });

        group.MapDelete("/{id:int}", async (int id, AppDbContext db) =>
        {
            var task = await db.Tasks.FindAsync(id);
            if (task is null)
            {
                return Results.NotFound();
            }

            db.Tasks.Remove(task);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        return app;
    }
}
