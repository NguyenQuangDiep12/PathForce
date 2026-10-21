namespace PathForge.Application.History;

public interface IHistoryService
{
    Task<IReadOnlyList<HistorySearchResult>> SearchAsync(string keyword, CancellationToken cancellationToken = default);
}
