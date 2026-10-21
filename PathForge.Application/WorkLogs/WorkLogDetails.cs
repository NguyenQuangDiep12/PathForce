using PathForge.Domain.Enums;
using PathForge.Domain.Extensions;

namespace PathForge.Application.WorkLogs;

public sealed record WorkLogDetails(
    int Id,
    int ProblemId,
    string ProblemTitle,
    string TaskTitle,
    int? PlannedDurationMinutes,
    DateTime StartedAt,
    DateTime? EndedAt,
    WorkLogStatus Status,
    int ActiveSeconds,
    DateTime? LastResumedAt,
    string? WhatWasDone,
    string? Result,
    string? Learned,
    string? Blocker,
    string? NextStep)
{
    public string StatusText => Status switch
    {
        WorkLogStatus.Running => "Đang thực hiện",
        WorkLogStatus.Paused => "Đang tạm dừng",
        WorkLogStatus.AwaitingReport => "Chờ báo cáo",
        WorkLogStatus.Completed => "Đã lưu",
        WorkLogStatus.Abandoned => "Đã bỏ",
        _ => string.Empty
    };

    public string DateText => StartedAt.ToLocalTimeFromUtc().ToString("dd/MM/yyyy HH:mm");
    public string Summary => new[] { Result, Learned, Blocker, NextStep }.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? "Chưa có báo cáo";

    public int ElapsedSeconds(DateTime utcNow) => ActiveSeconds +
        (Status == WorkLogStatus.Running && LastResumedAt.HasValue
            ? Math.Max(0, (int)(utcNow - LastResumedAt.Value).TotalSeconds)
            : 0);
}
