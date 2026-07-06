using Auth2Demo.Application.Services.Admin;

namespace Auth2Demo.Infrastructure.Services.Admin;

public sealed class ScopeAdminService : IScopeAdminService
{
    private readonly IScopeAdminRepository _scopes;

    public ScopeAdminService(IScopeAdminRepository scopes)
    {
        _scopes = scopes;
    }

    public Task<IReadOnlyList<ScopeListItemData>> ListAsync(int count = 100, int offset = 0)
    {
        return _scopes.ListAsync(count, offset);
    }

    public Task CreateIfMissingAsync(ScopeCreateData data)
    {
        return _scopes.CreateIfMissingAsync(data);
    }
}
