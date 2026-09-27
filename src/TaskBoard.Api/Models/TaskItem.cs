namespace TaskBoard.Api.Models;

/// <summary>
/// Deliberately tiny — the point of this repo is the container story, not
/// the domain. One table is enough to prove the API can talk to a real
/// SQL Server instance running in its own container, not an in-memory
/// stand-in.
/// </summary>
public sealed class TaskItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsComplete { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
