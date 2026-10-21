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
            problem.AnalysisWhat, problem.AnalysisWhy, problem.AnalysisWho, problem.AnalysisWhere,
            problem.AnalysisWhen, problem.AnalysisHow, problem.SuccessCriteria, problem.NextAction)).ToList();
    }

    public async Task<Problem?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Problems.AsNoTracking().FirstOrDefaultAsync(problem => problem.Id == id, cancellationToken);
    }

    public async Task<int> CountEvidenceAsync(int problemId, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var relativePaths = await context.Evidence.AsNoTracking().Where(item => item.ProblemId == problemId)
            .Select(item => item.RelativePath).ToListAsync(cancellationToken);
        var validCount = 0;
        foreach (var relativePath in relativePaths)
        {
            try
            {
                var path = _fileStorage.GetAbsolutePath(relativePath);
                if (File.Exists(path) && _fileStorage.GetLength(path) <= 5 * 1024 * 1024 && _fileStorage.HasSupportedImageSignature(path))
                    validCount++;
            }
            catch (IOException)
            {
                // A missing or unreadable file is not valid evidence for resolving a problem.
            }
            catch (UnauthorizedAccessException)
            {
                // A file that cannot be verified does not satisfy the evidence requirement.
            }
            catch (InvalidOperationException)
            {
                // An invalid stored path is not valid evidence for resolving a problem.
            }
        }
        return validCount;
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

    public async Task<IReadOnlyList<string>> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var problem = await context.Problems.FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (problem is null)
            return Array.Empty<string>();
        var evidencePaths = await context.Evidence.AsNoTracking().Where(item => item.ProblemId == id)
            .Select(item => item.RelativePath).ToListAsync(cancellationToken);
        var stagedPaths = new List<(string Original, string Staged)>();

        try
        {
            foreach (var relativePath in evidencePaths)
            {
                var staged = await _fileStorage.StageDeleteAsync(relativePath, cancellationToken);
                if (staged is not null)
                    stagedPaths.Add((relativePath, staged));
            }
        }
        catch
        {
            await RestoreStagedPathsAsync(stagedPaths, CancellationToken.None);
            throw;
        }

        try
        {
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            context.Problems.Remove(problem);
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await RestoreStagedPathsAsync(stagedPaths, CancellationToken.None);
            throw;
        }

        var pendingCleanup = new List<string>();
        foreach (var path in stagedPaths)
        {
            try
            {
                await _fileStorage.DeleteAsync(path.Staged, cancellationToken);
            }
            catch (Exception)
            {
                pendingCleanup.Add(path.Staged);
            }
        }
        return pendingCleanup;
    }

    private async Task RestoreStagedPathsAsync(
        IReadOnlyList<(string Original, string Staged)> stagedPaths, CancellationToken cancellationToken)
    {
        foreach (var path in stagedPaths.Reverse())
            await _fileStorage.RestoreStagedAsync(path.Staged, path.Original, cancellationToken);
    }

    private static bool Matches(string keyword, params string?[] fields) =>
        fields.Any(field => field?.IndexOf(keyword, StringComparison.CurrentCultureIgnoreCase) >= 0);
}
