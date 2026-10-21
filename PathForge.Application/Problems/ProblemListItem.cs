using PathForge.Domain.Enums;
using PathForge.Domain.Extensions;

namespace PathForge.Application.Problems;

public sealed record ProblemListItem(
    int Id,
    string Title,
    string Goal,
    ProblemStatus Status,
    DateTime UpdatedAt)
{
    public string UpdatedText => UpdatedAt.ToLocalTimeFromUtc().ToString("dd/MM/yyyy HH:mm");
    public string StatusText => Status switch
    {
        ProblemStatus.NotStarted => "Chưa bắt đầu",
        ProblemStatus.InProgress => "Đang thực hiện",
        ProblemStatus.Blocked => "Bị chặn",
        ProblemStatus.Resolved => "Đã hoàn thành",
        _ => string.Empty
    };
}
