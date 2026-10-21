namespace PathForge.Application.KnowledgeDocuments;

public interface IKnowledgeDocumentService
{
    Task<IReadOnlyList<KnowledgeDocumentListItem>> SearchAsync(string? title, CancellationToken cancellationToken = default);
    Task<KnowledgeDocumentDetails?> GetAsync(int id, CancellationToken cancellationToken = default);
    Task<KnowledgeDocumentSaveResult> SaveAsync(int? id, string title, string content, CancellationToken cancellationToken = default);
}
