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
    public IReadOnlyList<string> Endpoints { get; set; } = Array.Empty<string>();
    public bool RequirePkce { get; set; }
    public IReadOnlyList<ClientScopeOptionViewModel> AvailableScopes { get; set; } = Array.Empty<ClientScopeOptionViewModel>();
}
