namespace RDBMS.WebApi.Models;

/// <summary>
/// Data Transfer Object for Task entity
/// </summary>
public class TaskDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool Completed { get; set; }
    public string? Priority { get; set; }
    public DateTime CreatedAt { get; set; }
}