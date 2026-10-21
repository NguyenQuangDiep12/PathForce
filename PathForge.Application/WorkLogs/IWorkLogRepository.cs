using PathForge.Domain.Entities;

namespace PathForge.Application.WorkLogs;

public interface IWorkLogRepository
{
    Task<IReadOnlyList<WorkLog>> GetHistoryAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkLog>> GetByProblemAsync(int problemId, CancellationToken cancellationToken = default);
    Task<WorkLog?> GetOpenAsync(CancellationToken cancellationToken = default);
    Task<WorkLog?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> HasRunningAsync(CancellationToken cancellationToken = default);
    Task<int> AddAsync(WorkLog workLog, CancellationToken cancellationToken = default);
    Task UpdateAsync(WorkLog workLog, CancellationToken cancellationToken = default);
}
