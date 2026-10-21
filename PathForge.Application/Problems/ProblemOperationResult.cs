namespace PathForge.Application.Problems;

public sealed record ProblemOperationResult(bool Succeeded, int? ProblemId, string? Error, string? Warning = null)
{
    public static ProblemOperationResult Success(int id, string? warning = null) => new(true, id, null, warning);
    public static ProblemOperationResult Failure(string error) => new(false, null, error);
}
