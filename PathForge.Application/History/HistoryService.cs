using PathForge.Application.Evidence;

namespace PathForge.Application.History;

public sealed class HistoryService : IHistoryService
{
    private readonly IHistoryRepository _repository;
    private readonly IFileStorage _fileStorage;

    public HistoryService(IHistoryRepository repository, IFileStorage fileStorage)
    {
        _repository = repository;
        _fileStorage = fileStorage;
    }

    public async Task<IReadOnlyList<HistorySearchResult>> SearchAsync(string keyword, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(keyword))
            return Array.Empty<HistorySearchResult>();

        var problemsTask = _repository.SearchProblemsAsync(keyword, cancellationToken);
        var scopeItemsTask = _repository.SearchScopeItemsAsync(keyword, cancellationToken);
        var workLogsTask = _repository.SearchWorkLogsAsync(keyword, cancellationToken);
        var evidenceTask = _repository.SearchEvidenceAsync(keyword, cancellationToken);
        await Task.WhenAll(problemsTask, scopeItemsTask, workLogsTask, evidenceTask);

        var problems = await problemsTask;
        var scopeItems = await scopeItemsTask;
        var workLogs = await workLogsTask;
        var evidence = await evidenceTask;

        var results = new List<HistorySearchResult>();
        results.AddRange(problems.Select(problem => new HistorySearchResult(
            HistoryResultKind.Problem, problem.Title, problem.Goal, problem.UpdatedAt,
            problem.Id, null, null, null, null)));
        results.AddRange(scopeItems.Select(item => new HistorySearchResult(
            HistoryResultKind.ScopeItem, item.Title,
            item.Reason ?? item.Problem?.Title ?? "Mục phạm vi", item.UpdatedAt,
            item.ProblemId, item.Id, null, null, null)));
        results.AddRange(workLogs.Select(log => new HistorySearchResult(
            HistoryResultKind.WorkLog, log.TaskTitle,
            FirstNonEmpty(log.ScopeItemTitle is null ? null : "Mục phạm vi: " + log.ScopeItemTitle,
                log.WhatWasDone, log.Result, log.Learned, log.Blocker, log.NextStep, log.Problem?.Title ?? "Phiên làm việc"),
            log.StartedAt, log.ProblemId, log.ScopeItemId, log.Id, null, null)));
        results.AddRange(evidence.Select(item => new HistorySearchResult(
            HistoryResultKind.Evidence, item.DisplayName,
            item.Description ?? item.Problem?.Title ?? "Ảnh minh chứng", item.CreatedAt,
            item.ProblemId, null, item.WorkLogId, item.Id, _fileStorage.GetAbsolutePath(item.RelativePath))));

        return results.OrderByDescending(result => result.Date).ToList();
    }

    private static string FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;
}
