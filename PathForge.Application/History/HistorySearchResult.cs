namespace PathForge.Application.History;

using PathForge.Domain.Extensions;

public sealed record HistorySearchResult(
    HistoryResultKind Kind,
    string Title,
    string Context,
    DateTime Date,
    int ProblemId,
    int? ScopeItemId,
    int? WorkLogId,
    int? EvidenceId,
    string? AbsolutePath)
{
    public string DateText => Date.ToLocalTimeFromUtc().ToString("dd/MM/yyyy HH:mm");

    public string KindText => Kind switch
    {
        HistoryResultKind.Problem => "Bài toán",
        HistoryResultKind.ScopeItem => "Mục phạm vi",
        HistoryResultKind.WorkLog => "Phiên làm việc",
        HistoryResultKind.Evidence => "Minh chứng",
        _ => string.Empty
    };
}
