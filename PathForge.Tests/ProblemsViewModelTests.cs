using PathForge.Application.Evidence;
using PathForge.Application.Problems;
using PathForge.Application.ScopeItems;
using PathForge.Application.WorkLogs;
using PathForge.Desktop.Services;
using PathForge.Desktop.ViewModels;
using PathForge.Domain.Enums;
using Xunit;

namespace PathForge.Tests;

public sealed class ProblemsViewModelTests
{
    [Fact]
    public async Task SavingNewProblemClearsFormSoNextSaveCreatesAnotherProblem()
    {
        var problems = new FakeProblemService();
        var viewModel = CreateViewModel(problems);

        viewModel.Title = "Bài toán thứ nhất";
        viewModel.Goal = "Mục tiêu thứ nhất";
        Assert.True(await viewModel.SavePendingChangesAsync());

        Assert.Equal(string.Empty, viewModel.Title);
        Assert.Equal(string.Empty, viewModel.Goal);
        Assert.Null(viewModel.SelectedProblem);
        Assert.False(viewModel.HasUnsavedChanges);

        viewModel.Title = "Bài toán thứ hai";
        viewModel.Goal = "Mục tiêu thứ hai";
        Assert.True(await viewModel.SavePendingChangesAsync());

        Assert.Equal(string.Empty, viewModel.Title);
        Assert.Equal(string.Empty, viewModel.Goal);
        Assert.Null(viewModel.SelectedProblem);

        viewModel.Title = "Bài toán thứ ba";
        viewModel.Goal = "Mục tiêu thứ ba";
        Assert.True(await viewModel.SavePendingChangesAsync());

        Assert.Equal(new int?[] { null, null, null }, problems.SavedIds);
        Assert.Equal(3, problems.Count);
        Assert.Equal(string.Empty, viewModel.Title);
    }

    [Fact]
    public async Task SavingExistingProblemKeepsItOpenForFurtherEditing()
    {
        var problems = new FakeProblemService();
        var viewModel = CreateViewModel(problems);
        viewModel.Title = "Bài toán";
        viewModel.Goal = "Mục tiêu";
        Assert.True(await viewModel.SavePendingChangesAsync());

        await viewModel.OpenProblemAsync(1);
        viewModel.Title = "Bài toán đã chỉnh sửa";

        Assert.True(await viewModel.SavePendingChangesAsync());

        Assert.Equal(new int?[] { null, 1 }, problems.SavedIds);
        Assert.Equal("Bài toán đã chỉnh sửa", viewModel.Title);
        Assert.Equal(1, viewModel.SelectedProblem?.Id);
    }

    [Fact]
    public async Task StartingNewProblemFromAnExistingOneMakesNextSaveCreateInsteadOfUpdate()
    {
        var problems = new FakeProblemService();
        var viewModel = CreateViewModel(problems);
        viewModel.Title = "Bài toán cũ";
        viewModel.Goal = "Mục tiêu cũ";
        Assert.True(await viewModel.SavePendingChangesAsync());

        await viewModel.OpenProblemAsync(1);
        viewModel.FocusNewProblem();
        viewModel.Title = "Bài toán mới";
        viewModel.Goal = "Mục tiêu mới";

        Assert.True(viewModel.IsNew);
        Assert.Equal("Tạo bài toán", viewModel.SaveButtonText);
        Assert.True(await viewModel.SavePendingChangesAsync());

        Assert.Equal(new int?[] { null, null }, problems.SavedIds);
        Assert.Equal(2, problems.Count);
    }

    [Fact]
    public async Task SelectingProblemFromListLoadsSavedDetailsForEditing()
    {
        var problems = new FakeProblemService();
        var viewModel = CreateViewModel(problems);
        viewModel.Title = "Bài toán đã lưu";
        viewModel.Goal = "Mục tiêu đã lưu";
        viewModel.Description = "Mô tả đã lưu";
        viewModel.AnalysisWhy = "Lý do đã lưu";
        viewModel.SuccessCriteria = "Tiêu chí đã lưu";
        viewModel.NextAction = "Bước tiếp theo đã lưu";
        Assert.True(await viewModel.SavePendingChangesAsync());

        viewModel.FocusNewProblem();
        var savedListItem = Assert.Single(viewModel.Problems);
        await viewModel.SelectProblemAsync(savedListItem);

        Assert.False(viewModel.IsNew);
        Assert.Equal(savedListItem.Id, viewModel.SelectedProblem?.Id);
        Assert.Equal("Bài toán đã lưu", viewModel.Title);
        Assert.Equal("Mục tiêu đã lưu", viewModel.Goal);
        Assert.Equal("Mô tả đã lưu", viewModel.Description);
        Assert.Equal("Lý do đã lưu", viewModel.AnalysisWhy);
        Assert.Equal("Tiêu chí đã lưu", viewModel.SuccessCriteria);
        Assert.Equal("Bước tiếp theo đã lưu", viewModel.NextAction);
    }

    private static ProblemsViewModel CreateViewModel(FakeProblemService problems) => new(
        problems,
        new FakeScopeItemService(),
        new FakeEvidenceService(),
        new FakeWorkLogService(),
        new FakeDialogs(),
        new FakeEvidenceFilePicker(),
        new FakeEvidenceOpener());

    private sealed class FakeProblemService : IProblemService
    {
        private readonly List<ProblemDetails> _problems = new();
        private int _nextId = 1;

        public List<int?> SavedIds { get; } = new();
        public int Count => _problems.Count;

        public Task<IReadOnlyList<ProblemListItem>> SearchAsync(string? keyword, CancellationToken cancellationToken = default)
        {
            var results = _problems
                .Where(problem => string.IsNullOrWhiteSpace(keyword) || problem.Title.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                .Select(problem => new ProblemListItem(problem.Id, problem.Title, problem.Goal, problem.Status, problem.UpdatedAt))
                .ToArray();
            return Task.FromResult<IReadOnlyList<ProblemListItem>>(results);
        }

        public Task<ProblemDetails?> GetAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_problems.FirstOrDefault(problem => problem.Id == id));

        public Task<ProblemDetails?> GetCurrentAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<ProblemDetails?>(null);

        public Task<ProblemOperationResult> SaveAsync(int? id, ProblemDraft draft, CancellationToken cancellationToken = default)
        {
            SavedIds.Add(id);
            var now = DateTime.UtcNow;
            var existing = id.HasValue ? _problems.FirstOrDefault(problem => problem.Id == id.Value) : null;
            var saved = new ProblemDetails(
                id ?? _nextId++, draft.Title, draft.Goal, draft.Description,
                draft.AnalysisWhat, draft.AnalysisWhy, draft.AnalysisWho, draft.AnalysisWhere,
                draft.AnalysisWhen, draft.AnalysisHow, draft.SuccessCriteria, draft.NextAction,
                existing?.Status ?? ProblemStatus.NotStarted, draft.RequiresEvidence,
                existing?.CreatedAt ?? now, now);

            if (existing is null)
                _problems.Add(saved);
            else
                _problems[_problems.IndexOf(existing)] = saved;
            return Task.FromResult(ProblemOperationResult.Success(saved.Id));
        }

        public Task<ProblemOperationResult> ChangeStatusAsync(int id, ProblemStatus status, bool userConfirmedResolution = false, CancellationToken cancellationToken = default) =>
            Task.FromResult(ProblemOperationResult.Success(id));

        public Task<ProblemOperationResult> DeleteAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(ProblemOperationResult.Success(id));
    }

    private sealed class FakeScopeItemService : IScopeItemService
    {
        public Task<IReadOnlyList<ScopeItemDetails>> GetByProblemAsync(int problemId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ScopeItemDetails>>(Array.Empty<ScopeItemDetails>());

        public Task<(bool Succeeded, int? Id, string? Error)> SaveAsync(int problemId, int? id, ScopeItemDraft draft, CancellationToken cancellationToken = default) =>
            Task.FromResult<(bool, int?, string?)>((true, id ?? 1, null));

        public Task<(bool Succeeded, string? Error)> ChangeStatusAsync(int id, ScopeItemStatus status, CancellationToken cancellationToken = default) =>
            Task.FromResult<(bool, string?)>((true, null));

        public Task<(bool Succeeded, string? Error)> DeleteAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult<(bool, string?)>((true, null));
    }

    private sealed class FakeEvidenceService : IEvidenceService
    {
        public Task<IReadOnlyList<EvidenceDetails>> GetByProblemAsync(int problemId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<EvidenceDetails>>(Array.Empty<EvidenceDetails>());

        public Task<(bool Succeeded, int? Id, string? Error)> AddAsync(int problemId, int? workLogId, string sourcePath, string? description, CancellationToken cancellationToken = default) =>
            Task.FromResult<(bool, int?, string?)>((true, 1, null));

        public Task<(bool Succeeded, string? Error)> DeleteAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult<(bool, string?)>((true, null));
    }

    private sealed class FakeWorkLogService : IWorkLogService
    {
        public Task<IReadOnlyList<WorkLogDetails>> GetHistoryAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<WorkLogDetails>>(Array.Empty<WorkLogDetails>());

        public Task<IReadOnlyList<WorkLogDetails>> GetByProblemAsync(int problemId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<WorkLogDetails>>(Array.Empty<WorkLogDetails>());

        public Task<WorkLogDetails?> GetOpenAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<WorkLogDetails?>(null);

        public Task<WorkLogDetails?> GetAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult<WorkLogDetails?>(null);

        public Task<WorkLogOperationResult> StartAsync(int problemId, string taskTitle, int? plannedDurationMinutes, int? scopeItemId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(WorkLogOperationResult.Success(1));

        public Task<WorkLogOperationResult> UpdateTaskTitleAsync(int id, string taskTitle, CancellationToken cancellationToken = default) =>
            Task.FromResult(WorkLogOperationResult.Success(id));

        public Task<WorkLogOperationResult> PauseAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(WorkLogOperationResult.Success(id));

        public Task<WorkLogOperationResult> ResumeAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(WorkLogOperationResult.Success(id));

        public Task<WorkLogOperationResult> EndAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(WorkLogOperationResult.Success(id));

        public Task<WorkLogOperationResult> AbandonAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(WorkLogOperationResult.Success(id));

        public Task<WorkLogOperationResult> SaveReportAsync(int id, WorkLogReportDraft report, CancellationToken cancellationToken = default) =>
            Task.FromResult(WorkLogOperationResult.Success(id));
    }

    private sealed class FakeDialogs : IUserDialogService
    {
        public bool Confirm(string title, string message) => true;
        public UnsavedChangesChoice AskToSaveChanges(string itemName) => UnsavedChangesChoice.Save;
        public void ShowMessage(string title, string message, bool isError = false) { }
    }

    private sealed class FakeEvidenceFilePicker : IEvidenceFilePicker
    {
        public string? ChooseImageFile() => null;
    }

    private sealed class FakeEvidenceOpener : IEvidenceOpener
    {
        public void Open(string path) { }
    }
}
