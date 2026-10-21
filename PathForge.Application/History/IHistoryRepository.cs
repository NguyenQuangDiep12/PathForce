using PathForge.Domain.Entities;
using EvidenceEntity = PathForge.Domain.Entities.Evidence;

namespace PathForge.Application.History;

public interface IHistoryRepository
{
    Task<IReadOnlyList<Problem>> SearchProblemsAsync(string keyword, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ScopeItem>> SearchScopeItemsAsync(string keyword, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkLog>> SearchWorkLogsAsync(string keyword, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EvidenceEntity>> SearchEvidenceAsync(string keyword, CancellationToken cancellationToken = default);
}
