namespace PathForge.Domain.Entities;

public sealed class Evidence
{
    public int Id { get; set; }
    public int ProblemId { get; set; }
    public int? WorkLogId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public Problem? Problem { get; set; }
    public WorkLog? WorkLog { get; set; }
}
