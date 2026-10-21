using Microsoft.EntityFrameworkCore;
using PathForge.Application.History;
using PathForge.Domain.Entities;
using PathForge.Infrastructure.Persistence;

namespace PathForge.Infrastructure.Repositories;

public sealed class EfHistoryRepository : IHistoryRepository
{
    private readonly IDbContextFactory<PathForgeDbContext> _contextFactory;

    public EfHistoryRepository(IDbContextFactory<PathForgeDbContext> contextFactory) => _contextFactory = contextFactory;

    public async Task<IReadOnlyList<Problem>> SearchProblemsAsync(string keyword, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var problems = await context.Problems.AsNoTracking().ToListAsync(cancellationToken);
        return problems.Where(problem => Matches(keyword, problem.Title, problem.Goal, problem.Description,
            problem.AnalysisWhat, problem.AnalysisWhy, problem.AnalysisWho, problem.AnalysisWhere,
            problem.AnalysisWhen, problem.AnalysisHow, problem.SuccessCriteria, problem.NextAction)).ToList();
    }

    public async Task<IReadOnlyList<ScopeItem>> SearchScopeItemsAsync(string keyword, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var items = await context.ScopeItems.AsNoTracking().Include(item => item.Problem).ToListAsync(cancellationToken);
        return items.Where(item => Matches(keyword, item.Title, item.Reason)).ToList();
    }

    public async Task<IReadOnlyList<WorkLog>> SearchWorkLogsAsync(string keyword, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var workLogs = await context.WorkLogs.AsNoTracking().Include(log => log.Problem).ToListAsync(cancellationToken);
        var scopeIds = workLogs.Where(log => log.ScopeItemId.HasValue)
            .Select(log => log.ScopeItemId.GetValueOrDefault()).Distinct().ToArray();
        if (scopeIds.Length > 0)
        {
            var titles = await context.ScopeItems.AsNoTracking().Where(item => scopeIds.Contains(item.Id))
                .ToDictionaryAsync(item => item.Id, item => item.Title, cancellationToken);
            foreach (var log in workLogs)
                if (log.ScopeItemId.HasValue && titles.TryGetValue(log.ScopeItemId.Value, out var title))
                    log.ScopeItemTitle = title;
        }
        return workLogs.Where(log => Matches(keyword, log.TaskTitle, log.WhatWasDone, log.Result,
            log.Learned, log.Blocker, log.NextStep)).ToList();
    }

    public async Task<IReadOnlyList<Evidence>> SearchEvidenceAsync(string keyword, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var items = await context.Evidence.AsNoTracking().Include(item => item.Problem).ToListAsync(cancellationToken);
        return items.Where(item => Matches(keyword, item.DisplayName, item.Description)).ToList();
    }

    private static bool Matches(string keyword, params string?[] fields)
    {
        var term = keyword.Trim();
        return fields.Any(field => field?.IndexOf(term, StringComparison.CurrentCultureIgnoreCase) >= 0);
    }
}
