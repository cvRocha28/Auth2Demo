namespace Auth2Demo.Application.Identity.Scopes;

public sealed record ScopeDto(string Id, string Name, string? DisplayName, string? Description, string? Resources);
