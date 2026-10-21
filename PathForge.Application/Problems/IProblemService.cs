namespace PathForge.Application.Problems;

public interface IProblemService
{
    Task<IReadOnlyList<ProblemListItem>> SearchAsync(string? keyword, CancellationToken cancellationToken = default);
    Task<ProblemDetails?> GetAsync(int id, CancellationToken cancellationToken = default);
    Task<ProblemDetails?> GetCurrentAsync(CancellationToken cancellationToken = default);
    Task<ProblemOperationResult> SaveAsync(int? id, ProblemDraft draft, CancellationToken cancellationToken = default);
    Task<ProblemOperationResult> ChangeStatusAsync(int id, PathForge.Domain.Enums.ProblemStatus status, CancellationToken cancellationToken = default);
    Task<ProblemOperationResult> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
