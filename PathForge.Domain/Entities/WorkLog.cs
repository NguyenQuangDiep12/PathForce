using PathForge.Domain.Enums;

namespace PathForge.Domain.Entities;

public sealed class WorkLog
{
    public int Id { get; set; }
    public int ProblemId { get; set; }
    public string TaskTitle { get; set; } = string.Empty;
    public int? PlannedDurationMinutes { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public int ActiveSeconds { get; set; }
    public DateTime? LastResumedAt { get; set; }
    public WorkLogStatus Status { get; set; } = WorkLogStatus.Running;
    public string? WhatWasDone { get; set; }
    public string? Result { get; set; }
    public string? Learned { get; set; }
    public string? Blocker { get; set; }
    public string? NextStep { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Problem? Problem { get; set; }
    public ICollection<Evidence> EvidenceItems { get; set; } = new List<Evidence>();
}
