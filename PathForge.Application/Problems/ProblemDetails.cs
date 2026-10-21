using PathForge.Domain.Enums;

namespace PathForge.Application.Problems;

public sealed record ProblemDetails(
    int Id,
    string Title,
    string Goal,
    string? Description,
    string? SuccessCriteria,
    string? NextAction,
    ProblemStatus Status,
    bool RequiresEvidence,
    DateTime CreatedAt,
    DateTime UpdatedAt);
