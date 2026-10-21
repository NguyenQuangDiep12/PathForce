using Microsoft.EntityFrameworkCore;
using PathForge.Application.WorkLogs;
using PathForge.Domain.Entities;
using PathForge.Domain.Enums;
using PathForge.Infrastructure.Persistence;

namespace PathForge.Infrastructure.Repositories;

public sealed class EfWorkLogRepository : IWorkLogRepository
{
    private readonly IDbContextFactory<PathForgeDbContext> _contextFactory;

    public EfWorkLogRepository(IDbContextFactory<PathForgeDbContext> contextFactory) => _contextFactory = contextFactory;

    public async Task<IReadOnlyList<WorkLog>> GetHistoryAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.WorkLogs.AsNoTracking().Include(log => log.Problem)
            .OrderByDescending(log => log.StartedAt).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WorkLog>> GetByProblemAsync(int problemId, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.WorkLogs.AsNoTracking().Include(log => log.Problem)
            .Where(log => log.ProblemId == problemId).OrderByDescending(log => log.StartedAt).ToListAsync(cancellationToken);
    }

    public async Task<WorkLog?> GetOpenAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.WorkLogs.AsNoTracking().Include(log => log.Problem)
            .Where(log => log.Status == WorkLogStatus.Running || log.Status == WorkLogStatus.Paused || log.Status == WorkLogStatus.AwaitingReport)
            .OrderByDescending(log => log.Status == WorkLogStatus.Running)
            .ThenByDescending(log => log.Status == WorkLogStatus.Paused)
            .ThenByDescending(log => log.StartedAt).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<WorkLog?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.WorkLogs.AsNoTracking().Include(log => log.Problem)
            .FirstOrDefaultAsync(log => log.Id == id, cancellationToken);
    }

    public async Task<bool> HasRunningAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.WorkLogs.AnyAsync(log => log.Status == WorkLogStatus.Running, cancellationToken);
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
