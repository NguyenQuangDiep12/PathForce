namespace PathForge.Application.Evidence;

public interface IEvidenceService
{
    Task<IReadOnlyList<EvidenceDetails>> GetByProblemAsync(int problemId, CancellationToken cancellationToken = default);
    Task<(bool Succeeded, int? Id, string? Error)> AddAsync(int problemId, int? workLogId, string sourcePath, string? description, CancellationToken cancellationToken = default);
    Task<(bool Succeeded, string? Error)> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
