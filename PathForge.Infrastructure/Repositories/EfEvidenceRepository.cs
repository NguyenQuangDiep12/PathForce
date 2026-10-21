using Microsoft.EntityFrameworkCore;
using PathForge.Application.Evidence;
using PathForge.Domain.Entities;
using PathForge.Infrastructure.Persistence;

namespace PathForge.Infrastructure.Repositories;

public sealed class EfEvidenceRepository : IEvidenceRepository
{
    private readonly IDbContextFactory<PathForgeDbContext> _contextFactory;

    public EfEvidenceRepository(IDbContextFactory<PathForgeDbContext> contextFactory) => _contextFactory = contextFactory;

    public async Task<IReadOnlyList<Evidence>> GetByProblemAsync(int problemId, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Evidence.AsNoTracking().Where(item => item.ProblemId == problemId)
            .OrderByDescending(item => item.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task<Evidence?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Evidence.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
    }

    public async Task<bool> WorkLogBelongsToProblemAsync(int workLogId, int problemId, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.WorkLogs.AnyAsync(log => log.Id == workLogId && log.ProblemId == problemId, cancellationToken);
    }

    public async Task<int> AddAsync(Evidence evidence, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        await context.Evidence.AddAsync(evidence, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return evidence.Id;
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var evidence = await context.Evidence.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (evidence is null)
            return;
        context.Evidence.Remove(evidence);
        await context.SaveChangesAsync(cancellationToken);
    }
}
