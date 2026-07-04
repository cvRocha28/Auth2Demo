using System.ComponentModel.DataAnnotations;
using Auth2Demo.Domain.Identity;

namespace Auth2Demo.Web.Areas.Admin.Models;

public sealed class ClientCreateViewModel
{
    [Required, MaxLength(100)] public string ClientId { get; set; } = string.Empty;
    [Required, MaxLength(200)] public string DisplayName { get; set; } = string.Empty;
    public ClientKind Kind { get; set; } = ClientKind.WebApplication;
    public string RedirectUris { get; set; } = "https://localhost:5002/signin-oidc";
    public string PostLogoutRedirectUris { get; set; } = "https://localhost:5002/signout-callback-oidc";
    public string Scopes { get; set; } = "openid profile email roles offline_access auth2demo.api";
    public bool RequireConsent { get; set; } = true;
    public bool GenerateSecret { get; set; } = true;
}
