using PathForge.Domain.Entities;

namespace PathForge.Application.ScopeItems;

public interface IScopeItemRepository
{
    Task<IReadOnlyList<ScopeItem>> GetByProblemAsync(int problemId, CancellationToken cancellationToken = default);
    Task<ScopeItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> ProblemExistsAsync(int problemId, CancellationToken cancellationToken = default);
    Task<int> AddAsync(ScopeItem item, CancellationToken cancellationToken = default);
    Task UpdateAsync(ScopeItem item, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
