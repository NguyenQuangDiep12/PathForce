namespace PathForge.Application.WorkLogs;

public interface IWorkLogService
{
    Task<IReadOnlyList<WorkLogDetails>> GetHistoryAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkLogDetails>> GetByProblemAsync(int problemId, CancellationToken cancellationToken = default);
    Task<WorkLogDetails?> GetOpenAsync(CancellationToken cancellationToken = default);
    Task<WorkLogDetails?> GetAsync(int id, CancellationToken cancellationToken = default);
    Task<WorkLogOperationResult> StartAsync(int problemId, string taskTitle, int? plannedDurationMinutes, CancellationToken cancellationToken = default);
    Task<WorkLogOperationResult> PauseAsync(int id, CancellationToken cancellationToken = default);
    Task<WorkLogOperationResult> ResumeAsync(int id, CancellationToken cancellationToken = default);
    Task<WorkLogOperationResult> EndAsync(int id, CancellationToken cancellationToken = default);
    Task<WorkLogOperationResult> AbandonAsync(int id, CancellationToken cancellationToken = default);
    Task<WorkLogOperationResult> SaveReportAsync(int id, WorkLogReportDraft report, CancellationToken cancellationToken = default);
}
