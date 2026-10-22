using PathForge.Domain.Entities;
using PathForge.Domain.Enums;

namespace PathForge.Application.Problems;

public interface IProblemRepository
{
    Task<IReadOnlyList<Problem>> SearchAsync(string? keyword, CancellationToken cancellationToken = default);
    async Task<Problem?> GetCurrentAsync(CancellationToken cancellationToken = default) =>
        (await SearchAsync(null, cancellationToken))
            .Where(problem => problem.Status != ProblemStatus.Resolved)
            .OrderByDescending(problem => problem.Status == ProblemStatus.InProgress || problem.Status == ProblemStatus.Blocked)
            .ThenByDescending(problem => problem.UpdatedAt)
            .FirstOrDefault();
    Task<Problem?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<int> CountEvidenceAsync(int problemId, CancellationToken cancellationToken = default);
    Task<int> AddAsync(Problem problem, CancellationToken cancellationToken = default);
    Task UpdateAsync(Problem problem, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
