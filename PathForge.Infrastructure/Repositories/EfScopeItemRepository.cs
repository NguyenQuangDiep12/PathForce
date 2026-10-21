using Microsoft.EntityFrameworkCore;
using PathForge.Application.ScopeItems;
using PathForge.Domain.Entities;
using PathForge.Infrastructure.Persistence;

namespace PathForge.Infrastructure.Repositories;

public sealed class EfScopeItemRepository : IScopeItemRepository
{
    private readonly IDbContextFactory<PathForgeDbContext> _contextFactory;

    public EfScopeItemRepository(IDbContextFactory<PathForgeDbContext> contextFactory) => _contextFactory = contextFactory;

    public async Task<IReadOnlyList<ScopeItem>> GetByProblemAsync(int problemId, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.ScopeItems.AsNoTracking().Where(item => item.ProblemId == problemId)
            .OrderBy(item => item.Status).ThenBy(item => item.UpdatedAt).ToListAsync(cancellationToken);
    }

    public async Task<ScopeItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.ScopeItems.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
    }

    public async Task<bool> ProblemExistsAsync(int problemId, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Problems.AnyAsync(problem => problem.Id == problemId, cancellationToken);
    }

    public async Task<int> AddAsync(ScopeItem item, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        await context.ScopeItems.AddAsync(item, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return item.Id;
    }

    public async Task UpdateAsync(ScopeItem item, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        context.ScopeItems.Update(item);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var item = await context.ScopeItems.FirstOrDefaultAsync(scopeItem => scopeItem.Id == id, cancellationToken);
        if (item is null)
            return;
        context.ScopeItems.Remove(item);
        await context.SaveChangesAsync(cancellationToken);
    }
}
