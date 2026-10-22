using PathForge.Domain.Enums;

namespace PathForge.Application.WorkLogs;

public interface IWorkLogService
{
    Task<IReadOnlyList<WorkLogDetails>> GetHistoryAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkLogDetails>> GetByProblemAsync(int problemId, CancellationToken cancellationToken = default);
    Task<WorkLogDetails?> GetOpenAsync(CancellationToken cancellationToken = default);
    async Task<WorkLogDetails?> GetLatestCompletedAsync(int problemId, CancellationToken cancellationToken = default) =>
        (await GetByProblemAsync(problemId, cancellationToken))
            .FirstOrDefault(log => log.Status == WorkLogStatus.Completed);
    Task<WorkLogDetails?> GetAsync(int id, CancellationToken cancellationToken = default);
    Task<WorkLogOperationResult> StartAsync(int problemId, string taskTitle, int? plannedDurationMinutes, int? scopeItemId = null, CancellationToken cancellationToken = default);
    Task<WorkLogOperationResult> UpdateTaskTitleAsync(int id, string taskTitle, CancellationToken cancellationToken = default);
    Task<WorkLogOperationResult> PauseAsync(int id, CancellationToken cancellationToken = default);
    Task<WorkLogOperationResult> ResumeAsync(int id, CancellationToken cancellationToken = default);
    Task<WorkLogOperationResult> EndAsync(int id, CancellationToken cancellationToken = default);
    Task<WorkLogOperationResult> AbandonAsync(int id, CancellationToken cancellationToken = default);
    Task<WorkLogOperationResult> SaveReportAsync(int id, WorkLogReportDraft report, CancellationToken cancellationToken = default);
}
