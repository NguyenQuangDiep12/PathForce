using PathForge.Domain.Entities;

namespace PathForge.Application.KnowledgeDocuments;

public interface IKnowledgeDocumentRepository
{
    Task<IReadOnlyList<KnowledgeDocument>> SearchAsync(string? title, CancellationToken cancellationToken = default);
    Task<KnowledgeDocument?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<int> AddAsync(KnowledgeDocument document, CancellationToken cancellationToken = default);
    Task UpdateAsync(KnowledgeDocument document, CancellationToken cancellationToken = default);
}
