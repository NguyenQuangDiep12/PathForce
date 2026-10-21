namespace PathForge.Application.WorkLogs;

public sealed record WorkLogOperationResult(bool Succeeded, int? WorkLogId, string? Error)
{
    public static WorkLogOperationResult Success(int id) => new(true, id, null);
    public static WorkLogOperationResult Failure(string error) => new(false, null, error);
}
