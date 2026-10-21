namespace PathForge.Application.Evidence;

public sealed record EvidenceDetails(int Id, int ProblemId, int? WorkLogId, string DisplayName, string AbsolutePath, string? Description, DateTime CreatedAt);
