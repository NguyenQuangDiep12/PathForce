using PathForge.Domain.Enums;

namespace PathForge.Application.ScopeItems;

public sealed record ScopeItemDetails(int Id, int ProblemId, string Title, string? Reason, ScopeItemStatus Status)
{
    public string StatusText => Status switch
    {
        ScopeItemStatus.InScope => "Trong phạm vi",
        ScopeItemStatus.Backlog => "Để sau",
        ScopeItemStatus.Done => "Đã xử lý",
        ScopeItemStatus.Excluded => "Đã loại khỏi phạm vi",
        _ => string.Empty
    };
}
