using Microsoft.EntityFrameworkCore;
using PathForge.Application.Problems;
using PathForge.Application.Evidence;
using PathForge.Domain.Entities;
using PathForge.Infrastructure.Persistence;

namespace PathForge.Infrastructure.Repositories;

public sealed class EfProblemRepository : IProblemRepository
{
    private readonly IDbContextFactory<PathForgeDbContext> _contextFactory;
    private readonly IFileStorage _fileStorage;

    public EfProblemRepository(IDbContextFactory<PathForgeDbContext> contextFactory, IFileStorage fileStorage)
    {
        _contextFactory = contextFactory;
        _fileStorage = fileStorage;
    }

    public async Task<IReadOnlyList<Problem>> SearchAsync(string? keyword, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var query = context.Problems.AsNoTracking();
        var problems = await query.OrderByDescending(problem => problem.UpdatedAt).ToListAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(keyword))
            return problems;
        var term = keyword.Trim();
        return problems.Where(problem => Matches(term, problem.Title, problem.Goal, problem.Description,
            problem.SuccessCriteria, problem.NextAction)).ToList();
    }

    public async Task<Problem?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Problems.AsNoTracking().FirstOrDefaultAsync(problem => problem.Id == id, cancellationToken);
    }

    public async Task<int> CountEvidenceAsync(int problemId, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Evidence.CountAsync(item => item.ProblemId == problemId, cancellationToken);
    }

    public async Task<int> AddAsync(Problem problem, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        await context.Problems.AddAsync(problem, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return problem.Id;
    }

    public async Task UpdateAsync(Problem problem, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        context.Problems.Update(problem);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var problem = await context.Problems.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (problem is null)
            return;
        var evidencePaths = await context.Evidence.AsNoTracking().Where(item => item.ProblemId == id)
            .Select(item => item.RelativePath).ToListAsync(cancellationToken);
        foreach (var relativePath in evidencePaths)
            await _fileStorage.DeleteAsync(relativePath, cancellationToken);
        context.Problems.Remove(problem);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static bool Matches(string keyword, params string?[] fields) =>
        fields.Any(field => field?.IndexOf(keyword, StringComparison.CurrentCultureIgnoreCase) >= 0);
}
