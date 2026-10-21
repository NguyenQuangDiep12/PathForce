using PathForge.Domain.Enums;

namespace PathForge.Application.ScopeItems;

public sealed record ScopeItemDraft(string Title, string? Reason, ScopeItemStatus Status = ScopeItemStatus.InScope);
