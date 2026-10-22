using PathForge.Domain.Entities;

namespace PathForge.Application.KnowledgeDocuments;

public interface IKnowledgeDocumentRepository
{
    Task<IReadOnlyList<KnowledgeDocument>> SearchAsync(string? title, CancellationToken cancellationToken = default);
    Task<KnowledgeDocument?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default) =>
        await GetByIdAsync(id, cancellationToken) is not null;
    Task<int> AddAsync(KnowledgeDocument document, CancellationToken cancellationToken = default);
    Task UpdateAsync(KnowledgeDocument document, CancellationToken cancellationToken = default);
}
