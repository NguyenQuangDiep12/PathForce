namespace PathForge.Application.Problems;

public sealed class ProblemDraft
{
    public string Title { get; set; } = string.Empty;
    public string Goal { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? SuccessCriteria { get; set; }
    public string? NextAction { get; set; }
    public bool RequiresEvidence { get; set; }
}
