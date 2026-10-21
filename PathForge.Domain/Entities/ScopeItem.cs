using PathForge.Domain.Enums;

namespace PathForge.Domain.Entities;

public sealed class ScopeItem
{
    public int Id { get; set; }
    public int ProblemId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public ScopeItemStatus Status { get; set; } = ScopeItemStatus.InScope;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Problem? Problem { get; set; }
}
