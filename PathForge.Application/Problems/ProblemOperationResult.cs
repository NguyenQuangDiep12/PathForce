namespace PathForge.Application.Problems;

public sealed record ProblemOperationResult(bool Succeeded, int? ProblemId, string? Error)
{
    public static ProblemOperationResult Success(int id) => new(true, id, null);
    public static ProblemOperationResult Failure(string error) => new(false, null, error);
}
