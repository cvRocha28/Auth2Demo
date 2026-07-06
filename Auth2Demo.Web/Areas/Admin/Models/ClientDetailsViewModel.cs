namespace Auth2Demo.Web.Areas.Admin.Models;

public sealed class ClientDetailsViewModel
{
    public string ClientId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string ClientType { get; set; } = string.Empty;
    public string ConsentType { get; set; } = string.Empty;
    public IReadOnlyList<string> RedirectUris { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> PostLogoutRedirectUris { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> AllowedScopes { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> GrantTypes { get; set; } = Array.Empty<string>();
    public IReadOnlyList<ClientSecretViewModel> Secrets { get; set; } = Array.Empty<ClientSecretViewModel>();
    public IReadOnlyList<ClientRequiredClaimViewModel> RequiredClaims { get; set; } = Array.Empty<ClientRequiredClaimViewModel>();
    public IReadOnlyList<string> Endpoints { get; set; } = Array.Empty<string>();
    public bool RequirePkce { get; set; }
    public IReadOnlyList<ClientScopeOptionViewModel> AvailableScopes { get; set; } = Array.Empty<ClientScopeOptionViewModel>();
}

public sealed class ClientSecretViewModel
{
    public string Id { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class ClientRequiredClaimViewModel
{
    public string Type { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
