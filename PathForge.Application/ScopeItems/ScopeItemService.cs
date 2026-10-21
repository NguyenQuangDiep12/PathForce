using PathForge.Domain.Entities;
using PathForge.Domain.Enums;

namespace PathForge.Application.ScopeItems;

public sealed class ScopeItemService : IScopeItemService
{
    private readonly IScopeItemRepository _repository;

    public ScopeItemService(IScopeItemRepository repository) => _repository = repository;

    public async Task<IReadOnlyList<ScopeItemDetails>> GetByProblemAsync(int problemId, CancellationToken cancellationToken = default)
    {
        var items = await _repository.GetByProblemAsync(problemId, cancellationToken);
        return items.Select(item => new ScopeItemDetails(item.Id, item.ProblemId, item.Title, item.Reason, item.Status)).ToList();
    }

    public async Task<(bool Succeeded, int? Id, string? Error)> SaveAsync(
        int problemId, int? id, ScopeItemDraft draft, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(draft.Title))
            return (false, null, "Vui lòng mô tả một vấn đề hoặc kết quả nhỏ cần đạt.");
        if (!await _repository.ProblemExistsAsync(problemId, cancellationToken))
            return (false, null, "Không tìm thấy bài toán chứa mục phạm vi này.");

        ScopeItem item;
        if (id.HasValue)
        {
            var existing = await _repository.GetByIdAsync(id.Value, cancellationToken);
            if (existing is null || existing.ProblemId != problemId)
                return (false, null, "Không tìm thấy mục phạm vi cần chỉnh sửa.");
            item = existing;
        }
        else
        {
            item = new ScopeItem { ProblemId = problemId, CreatedAt = DateTime.UtcNow };
        }

        item.Title = draft.Title.Trim();
        item.Reason = string.IsNullOrWhiteSpace(draft.Reason) ? null : draft.Reason.Trim();
        item.Status = draft.Status;
        item.UpdatedAt = DateTime.UtcNow;
        var savedId = id.HasValue
            ? await UpdateAndReturnIdAsync(item, cancellationToken)
            : await _repository.AddAsync(item, cancellationToken);
        return (true, savedId, null);
    }

    public async Task<(bool Succeeded, string? Error)> ChangeStatusAsync(int id, ScopeItemStatus status, CancellationToken cancellationToken = default)
    {
        var item = await _repository.GetByIdAsync(id, cancellationToken);
        if (item is null)
            return (false, "Không tìm thấy mục phạm vi.");
        item.Status = status;
        item.UpdatedAt = DateTime.UtcNow;
        await _repository.UpdateAsync(item, cancellationToken);
        return (true, null);
    }

    public async Task<(bool Succeeded, string? Error)> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        if (await _repository.GetByIdAsync(id, cancellationToken) is null)
            return (false, "Không tìm thấy mục phạm vi cần xóa.");
        await _repository.DeleteAsync(id, cancellationToken);
        return (true, null);
    }

    private async Task<int> UpdateAndReturnIdAsync(ScopeItem item, CancellationToken cancellationToken)
    {
        await _repository.UpdateAsync(item, cancellationToken);
        return item.Id;
    }
}
