using PathForge.Domain.Entities;
using EvidenceEntity = PathForge.Domain.Entities.Evidence;

namespace PathForge.Application.Evidence;

public interface IEvidenceRepository
{
    Task<IReadOnlyList<EvidenceEntity>> GetByProblemAsync(int problemId, CancellationToken cancellationToken = default);
    Task<EvidenceEntity?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> WorkLogBelongsToProblemAsync(int workLogId, int problemId, CancellationToken cancellationToken = default);
    Task<int> AddAsync(EvidenceEntity evidence, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
