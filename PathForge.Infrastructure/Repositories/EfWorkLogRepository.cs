using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using PathForge.Application.WorkLogs;
using PathForge.Domain.Entities;
using PathForge.Domain.Enums;
using PathForge.Infrastructure.Persistence;

namespace PathForge.Infrastructure.Repositories;

public sealed class EfWorkLogRepository : IWorkLogRepository
{
    private static readonly Expression<Func<WorkLog, WorkLog>> WorkLogDetailsProjection = log => new WorkLog
    {
        Id = log.Id,
        ProblemId = log.ProblemId,
        ScopeItemId = log.ScopeItemId,
        TaskTitle = log.TaskTitle,
        PlannedDurationMinutes = log.PlannedDurationMinutes,
        StartedAt = log.StartedAt,
        EndedAt = log.EndedAt,
        ActiveSeconds = log.ActiveSeconds,
        LastResumedAt = log.LastResumedAt,
        Status = log.Status,
        WhatWasDone = log.WhatWasDone,
        Result = log.Result,
        Learned = log.Learned,
        Blocker = log.Blocker,
        NextStep = log.NextStep,
        CreatedAt = log.CreatedAt,
        UpdatedAt = log.UpdatedAt,
        Problem = log.Problem == null ? null : new Problem { Title = log.Problem.Title }
    };

    private readonly IDbContextFactory<PathForgeDbContext> _contextFactory;

    public EfWorkLogRepository(IDbContextFactory<PathForgeDbContext> contextFactory) => _contextFactory = contextFactory;

    public async Task<IReadOnlyList<WorkLog>> GetHistoryAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var logs = await context.WorkLogs.AsNoTracking()
            .OrderByDescending(log => log.StartedAt)
            .Select(WorkLogDetailsProjection)
            .ToListAsync(cancellationToken);
        await LoadScopeTitlesAsync(context, logs, cancellationToken);
        return logs;
    }

    public async Task<IReadOnlyList<WorkLog>> GetByProblemAsync(int problemId, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var logs = await context.WorkLogs.AsNoTracking()
            .Where(log => log.ProblemId == problemId)
            .OrderByDescending(log => log.StartedAt)
            .Select(WorkLogDetailsProjection)
            .ToListAsync(cancellationToken);
        await LoadScopeTitlesAsync(context, logs, cancellationToken);
        return logs;
    }

    public async Task<WorkLog?> GetLatestCompletedAsync(int problemId, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var log = await context.WorkLogs.AsNoTracking()
            .Where(item => item.ProblemId == problemId && item.Status == WorkLogStatus.Completed)
            .OrderByDescending(item => item.StartedAt)
            .Select(WorkLogDetailsProjection)
            .FirstOrDefaultAsync(cancellationToken);
        if (log is not null)
            await LoadScopeTitlesAsync(context, new[] { log }, cancellationToken);
        return log;
    }

    public async Task<WorkLog?> GetOpenAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var log = await context.WorkLogs.AsNoTracking()
            .Where(log => log.Status == WorkLogStatus.Running || log.Status == WorkLogStatus.Paused || log.Status == WorkLogStatus.AwaitingReport)
            .OrderByDescending(log => log.Status == WorkLogStatus.Running)
            .ThenByDescending(log => log.Status == WorkLogStatus.Paused)
            .ThenByDescending(log => log.StartedAt)
            .Select(WorkLogDetailsProjection)
            .FirstOrDefaultAsync(cancellationToken);
        if (log is not null)
            await LoadScopeTitlesAsync(context, new[] { log }, cancellationToken);
        return log;
    }

    public async Task<WorkLog?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var log = await context.WorkLogs.AsNoTracking()
            .Select(WorkLogDetailsProjection)
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (log is not null)
            await LoadScopeTitlesAsync(context, new[] { log }, cancellationToken);
        return log;
    }

    public async Task<bool> HasRunningAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.WorkLogs.AnyAsync(log => log.Status == WorkLogStatus.Running, cancellationToken);
    }

    public async Task<bool> ScopeItemBelongsToProblemAsync(int scopeItemId, int problemId, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.ScopeItems.AsNoTracking()
            .AnyAsync(item => item.Id == scopeItemId && item.ProblemId == problemId, cancellationToken);
    }

    private static async Task LoadScopeTitlesAsync(
        PathForgeDbContext context, IReadOnlyCollection<WorkLog> logs, CancellationToken cancellationToken)
    {
        var ids = logs.Where(log => log.ScopeItemId.HasValue).Select(log => log.ScopeItemId.GetValueOrDefault()).Distinct().ToArray();
        if (ids.Length == 0)
            return;
        var titles = await context.ScopeItems.AsNoTracking().Where(item => ids.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, item => item.Title, cancellationToken);
        foreach (var log in logs)
            if (log.ScopeItemId.HasValue && titles.TryGetValue(log.ScopeItemId.Value, out var title))
                log.ScopeItemTitle = title;
    }

    public async Task<int> AddAsync(WorkLog workLog, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        await context.WorkLogs.AddAsync(workLog, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return workLog.Id;
    }

    public async Task UpdateAsync(WorkLog workLog, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        context.WorkLogs.Update(workLog);
        await context.SaveChangesAsync(cancellationToken);
    }
}
