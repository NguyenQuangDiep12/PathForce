using PathForge.Domain.Entities;

namespace PathForge.Application.KnowledgeDocuments;

public sealed class KnowledgeDocumentService : IKnowledgeDocumentService
{
    private const int MaximumTitleLength = 300;
    private readonly IKnowledgeDocumentRepository _repository;

    public KnowledgeDocumentService(IKnowledgeDocumentRepository repository) => _repository = repository;

    public async Task<IReadOnlyList<KnowledgeDocumentListItem>> SearchAsync(
        string? title, CancellationToken cancellationToken = default)
    {
        var documents = await _repository.SearchAsync(title, cancellationToken);
        return documents.Select(document => new KnowledgeDocumentListItem(
            document.Id, document.Title, document.CreatedAt, document.UpdatedAt)).ToList();
    }

    public async Task<KnowledgeDocumentDetails?> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        var document = await _repository.GetByIdAsync(id, cancellationToken);
        return document is null ? null : new KnowledgeDocumentDetails(
            document.Id, document.Title, document.Content, document.CreatedAt, document.UpdatedAt);
    }

    public async Task<KnowledgeDocumentSaveResult> SaveAsync(
        int? id, string title, string content, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(title))
            return KnowledgeDocumentSaveResult.Failure("Vui lòng nhập tên tài liệu.");
        if (title.Trim().Length > MaximumTitleLength)
            return KnowledgeDocumentSaveResult.Failure($"Tên tài liệu không được dài quá {MaximumTitleLength} ký tự.");

        try
        {
            var now = DateTime.UtcNow;
            KnowledgeDocument document;
            var isNew = !id.HasValue;
            if (id.HasValue)
            {
                document = await _repository.GetByIdAsync(id.Value, cancellationToken)
                    ?? throw new InvalidOperationException("Không tìm thấy tài liệu cần lưu.");
            }
            else
            {
                document = new KnowledgeDocument { CreatedAt = now };
            }

            document.Title = title.Trim();
            document.Content = content ?? string.Empty;
            document.UpdatedAt = now;

            var savedId = isNew
                ? await _repository.AddAsync(document, cancellationToken)
                : await UpdateAndReturnIdAsync(document, cancellationToken);
            return KnowledgeDocumentSaveResult.Success(savedId);
        }
        catch (InvalidOperationException exception)
        {
            return KnowledgeDocumentSaveResult.Failure(exception.Message);
        }
    }

    private async Task<int> UpdateAndReturnIdAsync(KnowledgeDocument document, CancellationToken cancellationToken)
    {
        await _repository.UpdateAsync(document, cancellationToken);
        return document.Id;
    }
}
