using PathForge.Application.Problems;
using PathForge.Domain.Entities;
using PathForge.Domain.Enums;

namespace PathForge.Application.WorkLogs;

public sealed class WorkLogService : IWorkLogService
{
    private readonly IWorkLogRepository _repository;
    private readonly IProblemService _problemService;

    public WorkLogService(IWorkLogRepository repository, IProblemService problemService)
    {
        _repository = repository;
        _problemService = problemService;
    }

    public async Task<IReadOnlyList<WorkLogDetails>> GetHistoryAsync(CancellationToken cancellationToken = default) =>
        (await _repository.GetHistoryAsync(cancellationToken)).Select(ToDetails).ToList();

    public async Task<IReadOnlyList<WorkLogDetails>> GetByProblemAsync(int problemId, CancellationToken cancellationToken = default) =>
        (await _repository.GetByProblemAsync(problemId, cancellationToken)).Select(ToDetails).ToList();

    public async Task<WorkLogDetails?> GetOpenAsync(CancellationToken cancellationToken = default)
    {
        var workLog = await _repository.GetOpenAsync(cancellationToken);
        return workLog is null ? null : ToDetails(workLog);
    }

    public async Task<WorkLogDetails?> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        var workLog = await _repository.GetByIdAsync(id, cancellationToken);
        return workLog is null ? null : ToDetails(workLog);
    }

    public async Task<WorkLogOperationResult> StartAsync(
        int problemId, string taskTitle, int? plannedDurationMinutes, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(taskTitle))
            return WorkLogOperationResult.Failure("Hãy mô tả một việc cụ thể để bắt đầu phiên.");
        if (plannedDurationMinutes is < 1 or > 1440)
            return WorkLogOperationResult.Failure("Thời lượng dự kiến không hợp lệ.");
        if (await _repository.HasRunningAsync(cancellationToken))
            return WorkLogOperationResult.Failure("Đang có một phiên chạy. Hãy tạm dừng, kết thúc hoặc bỏ phiên đó trước.");

        var problem = await _problemService.GetAsync(problemId, cancellationToken);
        if (problem is null)
            return WorkLogOperationResult.Failure("Không tìm thấy bài toán được chọn.");
        if (problem.Status == ProblemStatus.Resolved)
            return WorkLogOperationResult.Failure("Bài toán đã hoàn thành. Hãy mở lại bài toán trước khi bắt đầu phiên mới.");
        if (problem.Status != ProblemStatus.InProgress)
        {
            var statusResult = await _problemService.ChangeStatusAsync(problemId, ProblemStatus.InProgress, cancellationToken);
            if (!statusResult.Succeeded)
                return WorkLogOperationResult.Failure(statusResult.Error ?? "Không thể chuyển bài toán sang trạng thái đang thực hiện.");
        }

        var now = DateTime.UtcNow;
        var workLog = new WorkLog
        {
            ProblemId = problemId,
            TaskTitle = taskTitle.Trim(),
            PlannedDurationMinutes = plannedDurationMinutes,
            StartedAt = now,
            LastResumedAt = now,
            Status = WorkLogStatus.Running,
            CreatedAt = now,
            UpdatedAt = now
        };
        try
        {
            var id = await _repository.AddAsync(workLog, cancellationToken);
            return WorkLogOperationResult.Success(id);
        }
        catch (Exception)
        {
            return WorkLogOperationResult.Failure("Không thể bắt đầu phiên. Hãy kiểm tra xem phiên khác có đang chạy không rồi thử lại.");
        }
    }

    public Task<WorkLogOperationResult> PauseAsync(int id, CancellationToken cancellationToken = default) =>
        ChangeStatusAsync(id, WorkLogStatus.Running, WorkLogStatus.Paused, true, cancellationToken);

    public Task<WorkLogOperationResult> ResumeAsync(int id, CancellationToken cancellationToken = default) =>
        ChangeStatusAsync(id, WorkLogStatus.Paused, WorkLogStatus.Running, false, cancellationToken);

    public Task<WorkLogOperationResult> EndAsync(int id, CancellationToken cancellationToken = default) =>
        ChangeStatusAsync(id, null, WorkLogStatus.AwaitingReport, true, cancellationToken);

    public async Task<WorkLogOperationResult> AbandonAsync(int id, CancellationToken cancellationToken = default)
    {
        var workLog = await _repository.GetByIdAsync(id, cancellationToken);
        if (workLog is null || (workLog.Status != WorkLogStatus.Running && workLog.Status != WorkLogStatus.Paused))
            return WorkLogOperationResult.Failure("Phiên này không thể bỏ ở trạng thái hiện tại.");
        var now = DateTime.UtcNow;
        AccumulateActiveTime(workLog, now);
        workLog.Status = WorkLogStatus.Abandoned;
        workLog.EndedAt = now;
        workLog.UpdatedAt = now;
        await _repository.UpdateAsync(workLog, cancellationToken);
        return WorkLogOperationResult.Success(id);
    }

    public async Task<WorkLogOperationResult> SaveReportAsync(
        int id, WorkLogReportDraft report, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(report.Result) && string.IsNullOrWhiteSpace(report.Learned))
            return WorkLogOperationResult.Failure("Hãy ghi kết quả đạt được hoặc điều đã học trước khi lưu báo cáo.");

        var workLog = await _repository.GetByIdAsync(id, cancellationToken);
        if (workLog is null || (workLog.Status != WorkLogStatus.AwaitingReport && workLog.Status != WorkLogStatus.Completed))
            return WorkLogOperationResult.Failure("Không tìm thấy phiên đang chờ báo cáo.");

        workLog.WhatWasDone = Normalize(report.WhatWasDone);
        workLog.Result = Normalize(report.Result);
        workLog.Learned = Normalize(report.Learned);
        workLog.Blocker = Normalize(report.Blocker);
        workLog.NextStep = Normalize(report.NextStep);
        workLog.Status = WorkLogStatus.Completed;
        workLog.EndedAt ??= DateTime.UtcNow;
        workLog.UpdatedAt = DateTime.UtcNow;
        await _repository.UpdateAsync(workLog, cancellationToken);
        return WorkLogOperationResult.Success(id);
    }

    private async Task<WorkLogOperationResult> ChangeStatusAsync(
        int id, WorkLogStatus? expectedStatus, WorkLogStatus targetStatus, bool stopTimer, CancellationToken cancellationToken)
    {
        var workLog = await _repository.GetByIdAsync(id, cancellationToken);
        if (workLog is null)
            return WorkLogOperationResult.Failure("Không tìm thấy phiên làm việc.");
        if (expectedStatus.HasValue && workLog.Status != expectedStatus.Value)
            return WorkLogOperationResult.Failure("Trạng thái phiên đã thay đổi. Hãy tải lại màn hình rồi thử lại.");
        if (!expectedStatus.HasValue && workLog.Status != WorkLogStatus.Running && workLog.Status != WorkLogStatus.Paused)
            return WorkLogOperationResult.Failure("Phiên này không thể kết thúc ở trạng thái hiện tại.");
        if (targetStatus == WorkLogStatus.Running && await _repository.HasRunningAsync(cancellationToken))
            return WorkLogOperationResult.Failure("Đang có một phiên chạy khác. Hãy tạm dừng phiên đó trước.");

        var now = DateTime.UtcNow;
        if (stopTimer)
            AccumulateActiveTime(workLog, now);
        else
            workLog.LastResumedAt = now;
        workLog.Status = targetStatus;
        workLog.UpdatedAt = now;
        if (targetStatus == WorkLogStatus.AwaitingReport)
            workLog.EndedAt = now;
        await _repository.UpdateAsync(workLog, cancellationToken);
        return WorkLogOperationResult.Success(id);
    }

    private static void AccumulateActiveTime(WorkLog workLog, DateTime now)
    {
        if (workLog.LastResumedAt.HasValue)
            workLog.ActiveSeconds += Math.Max(0, (int)(now - workLog.LastResumedAt.Value).TotalSeconds);
        workLog.LastResumedAt = null;
    }

    private static string? Normalize(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();

    private static WorkLogDetails ToDetails(WorkLog workLog) => new(
        workLog.Id, workLog.ProblemId, workLog.Problem?.Title ?? "Bài toán", workLog.TaskTitle,
        workLog.PlannedDurationMinutes, workLog.StartedAt, workLog.EndedAt, workLog.Status,
        workLog.ActiveSeconds, workLog.LastResumedAt, workLog.WhatWasDone, workLog.Result,
        workLog.Learned, workLog.Blocker, workLog.NextStep);
}
