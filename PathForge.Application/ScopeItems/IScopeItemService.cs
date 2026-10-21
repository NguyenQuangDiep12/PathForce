using PathForge.Domain.Enums;

namespace PathForge.Application.ScopeItems;

public interface IScopeItemService
{
    Task<IReadOnlyList<ScopeItemDetails>> GetByProblemAsync(int problemId, CancellationToken cancellationToken = default);
    Task<(bool Succeeded, int? Id, string? Error)> SaveAsync(int problemId, int? id, ScopeItemDraft draft, CancellationToken cancellationToken = default);
    Task<(bool Succeeded, string? Error)> ChangeStatusAsync(int id, ScopeItemStatus status, CancellationToken cancellationToken = default);
    Task<(bool Succeeded, string? Error)> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
