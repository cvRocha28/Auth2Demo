namespace Auth2Demo.Application.Services.Admin;

public sealed record ScopeListItemData(string Name, string DisplayName, string? Description);
public sealed record ScopeCreateData(string Name, string DisplayName, string? Description, IReadOnlyCollection<string> Resources);

public interface IScopeAdminService
{
    Task<IReadOnlyList<ScopeListItemData>> ListAsync(int count = 100, int offset = 0);
    Task CreateIfMissingAsync(ScopeCreateData data);
}

public interface IScopeAdminRepository
{
    Task<IReadOnlyList<ScopeListItemData>> ListAsync(int count, int offset);
    Task CreateIfMissingAsync(ScopeCreateData data);
}
