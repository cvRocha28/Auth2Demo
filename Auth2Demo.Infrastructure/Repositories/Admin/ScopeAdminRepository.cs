using Auth2Demo.Application.Services.Admin;
using OpenIddict.Abstractions;

namespace Auth2Demo.Infrastructure.Repositories.Admin;

public sealed class ScopeAdminRepository : IScopeAdminRepository
{
    private readonly IOpenIddictScopeManager _scopeManager;

    public ScopeAdminRepository(IOpenIddictScopeManager scopeManager)
    {
        _scopeManager = scopeManager;
    }

    public async Task<IReadOnlyList<ScopeListItemData>> ListAsync(int count, int offset)
    {
        var scopes = new List<ScopeListItemData>();

        await foreach (var scope in _scopeManager.ListAsync(count, offset))
        {
            var name = await _scopeManager.GetNameAsync(scope) ?? string.Empty;
            var displayName = await _scopeManager.GetDisplayNameAsync(scope) ?? string.Empty;
            var description = await _scopeManager.GetDescriptionAsync(scope);
            scopes.Add(new ScopeListItemData(name, displayName, description));
        }

        return scopes.OrderBy(scope => scope.Name).ToArray();
    }

    public async Task CreateIfMissingAsync(ScopeCreateData data)
    {
        var name = data.Name.Trim();
        if (string.IsNullOrWhiteSpace(name) || await _scopeManager.FindByNameAsync(name) is not null)
        {
            return;
        }

        var descriptor = new OpenIddictScopeDescriptor
        {
            Name = name,
            DisplayName = data.DisplayName.Trim(),
            Description = string.IsNullOrWhiteSpace(data.Description) ? null : data.Description.Trim()
        };

        foreach (var resource in data.Resources
            .Where(resource => !string.IsNullOrWhiteSpace(resource))
            .Select(resource => resource.Trim())
            .Distinct(StringComparer.Ordinal))
        {
            descriptor.Resources.Add(resource);
        }

        await _scopeManager.CreateAsync(descriptor);
    }
}
