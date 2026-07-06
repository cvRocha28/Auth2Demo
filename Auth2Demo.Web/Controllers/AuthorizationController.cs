using System.Collections.Immutable;
using System.Security.Claims;
using Auth2Demo.Infrastructure.Identity;
using Auth2Demo.Web.Models.Authorization;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Microsoft.AspNetCore.WebUtilities;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;
using Auth2Demo.Web;

namespace Auth2Demo.Web.Controllers;

public sealed class AuthorizationController : Controller
{
    private const string RequiredClaimPermissionPrefix = "custom:required_claim:";

    private readonly IOpenIddictApplicationManager _applicationManager;
    private readonly IOpenIddictAuthorizationManager _authorizationManager;
    private readonly IOpenIddictScopeManager _scopeManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public AuthorizationController(
        IOpenIddictApplicationManager applicationManager,
        IOpenIddictAuthorizationManager authorizationManager,
        IOpenIddictScopeManager scopeManager,
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IStringLocalizer<SharedResource> localizer)
    {
        _applicationManager = applicationManager;
        _authorizationManager = authorizationManager;
        _scopeManager = scopeManager;
        _signInManager = signInManager;
        _userManager = userManager;
        _localizer = localizer;
    }

    [HttpGet("~/connect/authorize")]
    [HttpPost("~/connect/authorize")]
    [Authorize]
    public async Task<IActionResult> Authorize()
    {
        var request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException(_localizer["OpenIddictRequestNotFound"].Value);

        if (string.IsNullOrWhiteSpace(request.ClientId))
        {
            throw new InvalidOperationException(
                _localizer["ConsentPostMissingClientId"].Value);
        }

        var user = await _userManager.GetUserAsync(User)
            ?? throw new InvalidOperationException(_localizer["UserNotFound"].Value);

        var application = await _applicationManager.FindByClientIdAsync(request.ClientId)
            ?? throw new InvalidOperationException(string.Format(_localizer["InvalidClientFormat"].Value, request.ClientId));

        var applicationPermissions = await _applicationManager.GetPermissionsAsync(application);
        var missingClaim = GetMissingRequiredClaim(User, applicationPermissions);

        if (!string.IsNullOrWhiteSpace(missingClaim))
        {
            return Forbid(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        var applicationId = await _applicationManager.GetIdAsync(application)
            ?? throw new InvalidOperationException(_localizer["ApplicationIdNotFound"].Value);

        var userId = await _userManager.GetUserIdAsync(user);
        var requestedScopes = request.GetScopes();

        var authorizations = await _authorizationManager.FindAsync(
            subject: userId,
            client: applicationId,
            status: Statuses.Valid,
            type: AuthorizationTypes.Permanent,
            scopes: requestedScopes).ToListAsync();

        var consentType = await _applicationManager.GetConsentTypeAsync(application);

        if (HttpMethods.IsPost(Request.Method) && Request.Form.ContainsKey("deny"))
        {
            return Forbid(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        if (consentType == ConsentTypes.Explicit && !authorizations.Any())
        {
            var accepted = HttpMethods.IsPost(Request.Method) && Request.Form.ContainsKey("accept");

            if (!accepted)
            {
                return View("Consent", new ConsentViewModel
                {
                    ApplicationName = await _applicationManager.GetLocalizedDisplayNameAsync(application) ?? request.ClientId,
                    Scope = request.Scope ?? string.Empty,
                    ReturnUrl = Request.PathBase + Request.Path + Request.QueryString
                });
            }
        }

        var principal = await CreatePrincipalAsync(user, request);

        if (!authorizations.Any())
        {
            var authorization = await _authorizationManager.CreateAsync(
                principal: principal,
                subject: userId,
                client: applicationId,
                type: AuthorizationTypes.Permanent,
                scopes: principal.GetScopes());

            principal.SetAuthorizationId(await _authorizationManager.GetIdAsync(authorization));
        }
        else
        {
            principal.SetAuthorizationId(await _authorizationManager.GetIdAsync(authorizations.Last()));
        }

        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    [HttpPost("~/connect/authorize/deny")]
    [Authorize]
    [ValidateAntiForgeryToken]
    public IActionResult Deny(string returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl) || !Url.IsLocalUrl(returnUrl))
        {
            return BadRequest(_localizer["InvalidReturnUrl"].Value);
        }

        var queryIndex = returnUrl.IndexOf('?', StringComparison.Ordinal);
        var query = queryIndex >= 0 ? returnUrl[(queryIndex + 1)..] : string.Empty;
        var parameters = QueryHelpers.ParseQuery(query);

        if (!parameters.TryGetValue(Parameters.RedirectUri, out var redirectUri) ||
            string.IsNullOrWhiteSpace(redirectUri.ToString()))
        {
            return Forbid(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        var values = new Dictionary<string, string?>
        {
            [Parameters.Error] = Errors.AccessDenied,
            [Parameters.ErrorDescription] = _localizer["UserDeniedApplicationAccess"].Value
        };

        if (parameters.TryGetValue(Parameters.State, out var state))
        {
            values[Parameters.State] = state.ToString();
        }

        return Redirect(QueryHelpers.AddQueryString(redirectUri.ToString(), values));
    }

    [HttpPost("~/connect/token")]
    public async Task<IActionResult> Exchange()
    {
        var request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException(_localizer["OpenIddictRequestNotFound"].Value);

        if (request.IsClientCredentialsGrantType())
        {
            var identity = new ClaimsIdentity(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            identity.AddClaim(Claims.Subject, request.ClientId!, Destinations.AccessToken);
            identity.AddClaim(Claims.Name, request.ClientId!, Destinations.AccessToken);

            var principal = new ClaimsPrincipal(identity);
            principal.SetScopes(request.GetScopes());
            principal.SetResources(await _scopeManager.ListResourcesAsync(principal.GetScopes()).ToListAsync());

            return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        if (request.IsAuthorizationCodeGrantType() || request.IsRefreshTokenGrantType())
        {
            var result = await HttpContext.AuthenticateAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

            var userId = result.Principal?.GetClaim(Claims.Subject);
            var user = userId is null ? null : await _userManager.FindByIdAsync(userId);

            if (user is null || !await _signInManager.CanSignInAsync(user))
            {
                return Forbid(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            var principal = await CreatePrincipalAsync(user, request);

            return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        throw new InvalidOperationException(_localizer["UnsupportedGrantType"].Value);
    }

    [Authorize(AuthenticationSchemes = OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)]
    [HttpGet("~/connect/userinfo")]
    [HttpPost("~/connect/userinfo")]
    public async Task<IActionResult> UserInfo()
    {
        var user = await _userManager.FindByIdAsync(User.GetClaim(Claims.Subject)!);

        if (user is null)
        {
            return Challenge(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        var roles = await _userManager.GetRolesAsync(user);

        return Ok(new Dictionary<string, object?>
        {
            [Claims.Subject] = await _userManager.GetUserIdAsync(user),
            [Claims.Email] = user.Email,
            [Claims.Name] = user.DisplayName,
            [Claims.PreferredUsername] = user.UserName,
            [Claims.Role] = roles
        });
    }

    [HttpGet("~/connect/logout")]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();

        return SignOut(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private static string? GetMissingRequiredClaim(ClaimsPrincipal principal, IEnumerable<string> permissions)
    {
        foreach (var requiredClaim in ExtractRequiredClaims(permissions))
        {
            var hasClaim = principal.Claims.Any(claim =>
                IsSameClaimType(claim.Type, requiredClaim.Type) &&
                (string.IsNullOrWhiteSpace(requiredClaim.Value) || string.Equals(claim.Value, requiredClaim.Value, StringComparison.OrdinalIgnoreCase)));

            if (!hasClaim)
            {
                return string.IsNullOrWhiteSpace(requiredClaim.Value)
                    ? requiredClaim.Type
                    : $"{requiredClaim.Type}={requiredClaim.Value}";
            }
        }

        return null;
    }

    private static IEnumerable<(string Type, string Value)> ExtractRequiredClaims(IEnumerable<string> permissions)
    {
        foreach (var permission in permissions.Where(permission => permission.StartsWith(RequiredClaimPermissionPrefix, StringComparison.Ordinal)))
        {
            var value = permission[RequiredClaimPermissionPrefix.Length..];
            var separator = value.IndexOf('=');

            if (separator < 0)
            {
                yield return (DecodePart(value), string.Empty);
                continue;
            }

            yield return (DecodePart(value[..separator]), DecodePart(value[(separator + 1)..]));
        }
    }

    private static bool IsSameClaimType(string actualType, string requiredType)
    {
        return string.Equals(actualType, requiredType, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(actualType, "http://schemas.microsoft.com/ws/2008/06/identity/claims/role", StringComparison.OrdinalIgnoreCase) &&
               string.Equals(requiredType, Claims.Role, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(actualType, "http://schemas.microsoft.com/ws/2008/06/identity/claims/role", StringComparison.OrdinalIgnoreCase) &&
               string.Equals(requiredType, "role", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(actualType, Claims.Role, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(requiredType, "role", StringComparison.OrdinalIgnoreCase);
    }

    private static string DecodePart(string value) => Uri.UnescapeDataString(value ?? string.Empty);

    private async Task<ClaimsPrincipal> CreatePrincipalAsync(ApplicationUser user, OpenIddictRequest request)
    {
        var principal = await _signInManager.CreateUserPrincipalAsync(user);
        var userId = await _userManager.GetUserIdAsync(user);

        principal.SetClaim(Claims.Subject, userId);
        principal.SetClaim(Claims.Email, user.Email ?? string.Empty);
        principal.SetClaim(Claims.Name, user.DisplayName ?? user.UserName ?? user.Email ?? userId);
        principal.SetClaim(Claims.PreferredUsername, user.UserName ?? user.Email ?? userId);

        var roles = await _userManager.GetRolesAsync(user);
        principal.SetClaims(Claims.Role, roles.ToImmutableArray());

        principal.SetScopes(request.GetScopes());
        principal.SetResources(await _scopeManager.ListResourcesAsync(principal.GetScopes()).ToListAsync());

        foreach (var claim in principal.Claims)
        {
            claim.SetDestinations(GetDestinations(claim));
        }

        return principal;
    }

    private static IEnumerable<string> GetDestinations(Claim claim)
    {
        switch (claim.Type)
        {
            case Claims.Subject:
            case Claims.Name:
            case Claims.PreferredUsername:
            case Claims.Email:
            case Claims.Role:
                yield return Destinations.AccessToken;
                yield return Destinations.IdentityToken;
                break;

            default:
                yield return Destinations.AccessToken;
                break;
        }
    }
}
