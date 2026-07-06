using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Auth2Demo.Application.Identity.Clients;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Auth2Demo.Web.Areas.Admin.Models;

public sealed class ClientFormViewModel
{
    [Required, MaxLength(100)]
    public string ClientId { get; set; } = string.Empty;

    [ValidateNever]
    public string? OriginalClientId { get; set; }

    [Required, MaxLength(200)]
    public string DisplayName { get; set; } = string.Empty;

    public ClientKind Kind { get; set; } = ClientKind.WebApplication;

    public string ClientType { get; set; } = ClientTypes.Confidential;

    public string ConsentType { get; set; } = ConsentTypes.Explicit;

    public string RedirectUris { get; set; } = "https://localhost:7108/signin-oidc";

    public string PostLogoutRedirectUris { get; set; } = "https://localhost:7108/signout-callback-oidc";

    public string Scopes { get; set; } = "openid profile email roles offline_access";

    public List<string> RedirectUriItems { get; set; } = ["https://localhost:7108/signin-oidc"];

    public List<string> PostLogoutRedirectUriItems { get; set; } = ["https://localhost:7108/signout-callback-oidc"];

    public List<string> ScopeItems { get; set; } = ["openid", "profile", "email", "roles", "offline_access"];

    public List<string> GrantTypeItems { get; set; } = [GrantTypes.AuthorizationCode, GrantTypes.RefreshToken];

    [ValidateNever]
    public List<ClientSecretInputViewModel> SecretItems { get; set; } = [];

    [ValidateNever]
    public List<ClientRequiredClaimInputViewModel> RequiredClaimItems { get; set; } = [];

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


    public bool IsEdit { get; set; }

    public bool IsPublicClient => string.Equals(ClientType, ClientTypes.Public, StringComparison.Ordinal);

    public static ClientFormViewModel CreateDefault()
    {
        return new ClientFormViewModel
        {
            SecretItems =
            [
                new ClientSecretInputViewModel
                {
                    Description = "default",
                    Expiration = ClientSecretExpiration.Never
                }
            ]
        };
    }
}

public sealed class ClientSecretInputViewModel
{
    public string? Description { get; set; }
    public ClientSecretExpiration Expiration { get; set; } = ClientSecretExpiration.Never;
    [ValidateNever]
    public string? PlainTextSecret { get; set; }
    public Guid? Id { get; set; }
    public DateTimeOffset? ExpiresAtUtc { get; set; }
    public DateTimeOffset? RevokedAtUtc { get; set; }
    public bool Remove { get; set; }
    public bool IsExisting { get; set; }
    [ValidateNever]
    public string? CreatedAt { get; set; }
}

public sealed class ClientRequiredClaimInputViewModel
{
    public string? Type { get; set; }
    public string? Value { get; set; }
    public bool Remove { get; set; }
}

public enum ClientSecretExpiration
{
    Never = 0,
    Months6 = 6,
    Months12 = 12,
    Months24 = 24
}
