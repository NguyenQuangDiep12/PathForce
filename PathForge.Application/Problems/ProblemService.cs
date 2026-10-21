using PathForge.Domain.Entities;
using PathForge.Domain.Enums;
using PathForge.Domain.Rules;

namespace PathForge.Application.Problems;

public sealed class ProblemService : IProblemService
{
    private readonly IProblemRepository _repository;

    public ProblemService(IProblemRepository repository) => _repository = repository;

    public async Task<IReadOnlyList<ProblemListItem>> SearchAsync(string? keyword, CancellationToken cancellationToken = default)
    {
        var problems = await _repository.SearchAsync(keyword, cancellationToken);
        return problems.Select(ToListItem).ToList();
    }

    public async Task<ProblemDetails?> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        var problem = await _repository.GetByIdAsync(id, cancellationToken);
        return problem is null ? null : ToDetails(problem);
    }

    public async Task<ProblemDetails?> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        var problems = await _repository.SearchAsync(null, cancellationToken);
        var current = problems
            .Where(problem => problem.Status != ProblemStatus.Resolved)
            .OrderByDescending(problem => problem.Status == ProblemStatus.InProgress || problem.Status == ProblemStatus.Blocked)
            .ThenByDescending(problem => problem.UpdatedAt)
            .FirstOrDefault();
        return current is null ? null : ToDetails(current);
    }

    public async Task<ProblemOperationResult> SaveAsync(int? id, ProblemDraft draft, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(draft.Title))
            return ProblemOperationResult.Failure("Vui lòng nhập tên bài toán.");
        if (string.IsNullOrWhiteSpace(draft.Goal))
            return ProblemOperationResult.Failure("Vui lòng mô tả mục tiêu cần đạt.");

        Problem problem;
        var isNew = !id.HasValue;
        if (id.HasValue)
        {
            var existing = await _repository.GetByIdAsync(id.Value, cancellationToken);
            if (existing is null)
                return ProblemOperationResult.Failure("Không tìm thấy bài toán cần chỉnh sửa.");
            problem = existing;
        }
        else
        {
            problem = new Problem { Status = ProblemStatus.NotStarted, CreatedAt = DateTime.UtcNow };
        }

        problem.Title = draft.Title.Trim();
        problem.Goal = draft.Goal.Trim();
        problem.Description = Normalize(draft.Description);
        problem.AnalysisWhat = Normalize(draft.AnalysisWhat);
        problem.AnalysisWhy = Normalize(draft.AnalysisWhy);
        problem.AnalysisWho = Normalize(draft.AnalysisWho);
        problem.AnalysisWhere = Normalize(draft.AnalysisWhere);
        problem.AnalysisWhen = Normalize(draft.AnalysisWhen);
        problem.AnalysisHow = Normalize(draft.AnalysisHow);
        problem.SuccessCriteria = Normalize(draft.SuccessCriteria);
        problem.NextAction = Normalize(draft.NextAction);
        problem.RequiresEvidence = draft.RequiresEvidence;
        problem.UpdatedAt = DateTime.UtcNow;

        if (isNew)
            problem.Id = await _repository.AddAsync(problem, cancellationToken);
        else
            await _repository.UpdateAsync(problem, cancellationToken);
        return ProblemOperationResult.Success(problem.Id);
    }

    public async Task<ProblemOperationResult> ChangeStatusAsync(
        int id, ProblemStatus status, bool userConfirmedResolution = false, CancellationToken cancellationToken = default)
    {
        var problem = await _repository.GetByIdAsync(id, cancellationToken);
        if (problem is null)
            return ProblemOperationResult.Failure("Không tìm thấy bài toán.");
        if (!ProblemStatusRules.CanTransition(problem.Status, status))
            return ProblemOperationResult.Failure("Trạng thái mới không hợp lệ với trạng thái hiện tại.");
        if (status == ProblemStatus.Resolved && !userConfirmedResolution)
            return ProblemOperationResult.Failure("Hãy xác nhận rõ ràng rằng mục tiêu bài toán đã đạt trước khi hoàn thành.");
        if (status == ProblemStatus.Resolved && problem.RequiresEvidence &&
            await _repository.CountEvidenceAsync(id, cancellationToken) == 0)
            return ProblemOperationResult.Failure("Bài toán này yêu cầu minh chứng. Hãy thêm ít nhất một ảnh trước khi hoàn thành.");

        problem.Status = status;
        problem.UpdatedAt = DateTime.UtcNow;
        await _repository.UpdateAsync(problem, cancellationToken);
        return ProblemOperationResult.Success(problem.Id);
    }

    public async Task<ProblemOperationResult> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var problem = await _repository.GetByIdAsync(id, cancellationToken);
        if (problem is null)
            return ProblemOperationResult.Failure("Không tìm thấy bài toán cần xóa.");
        try
        {
            var pendingCleanup = await _repository.DeleteAsync(id, cancellationToken);
            var warning = pendingCleanup.Count == 0
                ? null
                : "Bài toán và dữ liệu trong SQLite đã được xóa. Không xóa được một số tệp ảnh tạm: " +
                  string.Join(", ", pendingCleanup) + ". Hãy xóa các tệp này trong thư mục Evidence của PathForge.";
            return ProblemOperationResult.Success(id, warning);
        }
        catch (Exception)
        {
            return ProblemOperationResult.Failure("Không hoàn tất xóa bài toán. Hãy kiểm tra quyền thư mục PathForge\\Evidence rồi thử lại; một số ảnh có thể đã được xử lý.");
        }
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static ProblemListItem ToListItem(Problem problem) =>
        new(problem.Id, problem.Title, problem.Goal, problem.Status, problem.UpdatedAt);

    private static ProblemDetails ToDetails(Problem problem) =>
        new(problem.Id, problem.Title, problem.Goal, problem.Description,
            problem.AnalysisWhat, problem.AnalysisWhy, problem.AnalysisWho,
            problem.AnalysisWhere, problem.AnalysisWhen, problem.AnalysisHow, problem.SuccessCriteria,
            problem.NextAction, problem.Status, problem.RequiresEvidence, problem.CreatedAt, problem.UpdatedAt);
}
