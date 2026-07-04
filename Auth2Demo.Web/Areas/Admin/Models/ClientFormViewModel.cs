using System.ComponentModel.DataAnnotations;
using Auth2Demo.Domain.Identity;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Auth2Demo.Web.Areas.Admin.Models;

public sealed class ClientFormViewModel
{
    [Required, MaxLength(100)]
    public string ClientId { get; set; } = string.Empty;

    public string OriginalClientId { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string DisplayName { get; set; } = string.Empty;

    public ClientKind Kind { get; set; } = ClientKind.WebApplication;

    public string ClientType { get; set; } = ClientTypes.Confidential;

    public string ConsentType { get; set; } = ConsentTypes.Explicit;

    public string RedirectUris { get; set; } = "https://localhost:7108/signin-oidc";

    public string PostLogoutRedirectUris { get; set; } = "https://localhost:7108/signout-callback-oidc";

    public string Scopes { get; set; } = "openid profile email roles offline_access";

    public bool AllowAuthorizationCode { get; set; } = true;

    public bool AllowRefreshToken { get; set; } = true;

    public bool AllowClientCredentials { get; set; }

    public bool RequirePkce { get; set; } = true;

    public bool AllowAuthorizationEndpoint { get; set; } = true;

    public bool AllowTokenEndpoint { get; set; } = true;

    public bool AllowEndSessionEndpoint { get; set; } = true;

    public bool AllowRevocationEndpoint { get; set; } = true;

    public bool AllowIntrospectionEndpoint { get; set; } = true;

    public bool GenerateSecret { get; set; } = true;

    public bool RegenerateSecret { get; set; }

    public bool IsEdit { get; set; }

    public bool IsPublicClient => string.Equals(ClientType, ClientTypes.Public, StringComparison.Ordinal);

    public static ClientFormViewModel CreateDefault()
    {
        return new ClientFormViewModel();
    }
}
