using Microsoft.EntityFrameworkCore;
using PathForge.Application.KnowledgeDocuments;
using PathForge.Domain.Entities;
using PathForge.Infrastructure.Persistence;

namespace PathForge.Infrastructure.Repositories;

public sealed class EfKnowledgeDocumentRepository : IKnowledgeDocumentRepository
{
    private readonly IDbContextFactory<PathForgeDbContext> _contextFactory;

    public EfKnowledgeDocumentRepository(IDbContextFactory<PathForgeDbContext> contextFactory) =>
        _contextFactory = contextFactory;

    public async Task<IReadOnlyList<KnowledgeDocument>> SearchAsync(
        string? title, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var documents = await context.KnowledgeDocuments.AsNoTracking()
            .OrderByDescending(document => document.UpdatedAt)
            .ToListAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(title))
            return documents;

        var keyword = title.Trim();
        return documents.Where(document => document.Title.IndexOf(keyword, StringComparison.CurrentCultureIgnoreCase) >= 0)
            .ToList();
    }

    public async Task<KnowledgeDocument?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.KnowledgeDocuments.AsNoTracking()
            .FirstOrDefaultAsync(document => document.Id == id, cancellationToken);
    }

    public async Task<int> AddAsync(KnowledgeDocument document, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        await context.KnowledgeDocuments.AddAsync(document, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return document.Id;
    }

    public async Task UpdateAsync(KnowledgeDocument document, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        context.KnowledgeDocuments.Update(document);
        await context.SaveChangesAsync(cancellationToken);
    }
}
