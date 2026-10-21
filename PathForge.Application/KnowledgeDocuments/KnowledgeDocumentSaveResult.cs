namespace PathForge.Application.KnowledgeDocuments;

public sealed record KnowledgeDocumentSaveResult(bool Succeeded, int? Id, string? Error)
{
    public static KnowledgeDocumentSaveResult Success(int id) => new(true, id, null);
    public static KnowledgeDocumentSaveResult Failure(string error) => new(false, null, error);
}
