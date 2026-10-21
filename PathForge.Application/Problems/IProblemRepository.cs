using PathForge.Domain.Entities;

namespace PathForge.Application.Problems;

public interface IProblemRepository
{
    Task<IReadOnlyList<Problem>> SearchAsync(string? keyword, CancellationToken cancellationToken = default);
    Task<Problem?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<int> CountEvidenceAsync(int problemId, CancellationToken cancellationToken = default);
    Task<int> AddAsync(Problem problem, CancellationToken cancellationToken = default);
    Task UpdateAsync(Problem problem, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
