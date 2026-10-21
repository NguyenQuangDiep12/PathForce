using PathForge.Domain.Enums;

namespace PathForge.Domain.Entities;

public sealed class Problem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Goal { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? AnalysisWhat { get; set; }
    public string? AnalysisWhy { get; set; }
    public string? AnalysisWho { get; set; }
    public string? AnalysisWhere { get; set; }
    public string? AnalysisWhen { get; set; }
    public string? AnalysisHow { get; set; }
    public string? SuccessCriteria { get; set; }
    public string? NextAction { get; set; }
    public ProblemStatus Status { get; set; } = ProblemStatus.NotStarted;
    public bool RequiresEvidence { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<ScopeItem> ScopeItems { get; set; } = new List<ScopeItem>();
    public ICollection<WorkLog> WorkLogs { get; set; } = new List<WorkLog>();
    public ICollection<Evidence> EvidenceItems { get; set; } = new List<Evidence>();
}
