using System.Collections.Immutable;
using System.Security.Claims;
using System.Text.Json;
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
using Auth2Demo.Application.Services.Admin;

namespace Auth2Demo.Web.Controllers;

public sealed class AuthorizationController : Controller
{
    private readonly IOpenIddictApplicationManager _applicationManager;
    private readonly IOpenIddictAuthorizationManager _authorizationManager;
    private readonly IOpenIddictScopeManager _scopeManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IStringLocalizer<SharedResource> _localizer;
    private readonly IEnterpriseApplicationAccessEvaluator _accessEvaluator;
    private readonly ILogger<AuthorizationController> _logger;

    public AuthorizationController(
        IOpenIddictApplicationManager applicationManager,
        IOpenIddictAuthorizationManager authorizationManager,
        IOpenIddictScopeManager scopeManager,
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IStringLocalizer<SharedResource> localizer,
        IEnterpriseApplicationAccessEvaluator accessEvaluator,
        ILogger<AuthorizationController> logger)
    {
        _applicationManager = applicationManager;
        _authorizationManager = authorizationManager;
        _scopeManager = scopeManager;
        _signInManager = signInManager;
        _userManager = userManager;
        _localizer = localizer;
        _accessEvaluator = accessEvaluator;
        _logger = logger;
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

        var applicationId = await _applicationManager.GetIdAsync(application)
            ?? throw new InvalidOperationException(_localizer["ApplicationIdNotFound"].Value);

        if (!Guid.TryParse(applicationId, out var enterpriseApplicationId))
        {
            throw new InvalidOperationException(_localizer["ApplicationIdNotFound"].Value);
        }

        var access = await _accessEvaluator.EvaluateAsync(user.Id, enterpriseApplicationId, HttpContext.RequestAborted);
        if (!access.IsAllowed)
        {
            var denial = ResolveAccessDenial(access.DenialReason);
            return await HandleAuthorizationDenialAsync(
                request,
                application,
                user,
                denial.Message,
                denial.IsAssignmentRequired);
        }

        var missingClaim = GetMissingRequiredClaim(application, User);
        if (!string.IsNullOrWhiteSpace(missingClaim))
        {
            var denialReason = string.Format(
                _localizer["AuthorizationMissingRequiredClaimFormat"].Value,
                missingClaim);

            return await HandleAuthorizationDenialAsync(
                request,
                application,
                user,
                denialReason,
                isAssignmentRequired: false);
        }

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
        foreach (var role in access.Roles)
        {
            if (principal.Identity is ClaimsIdentity claimsIdentity && !claimsIdentity.HasClaim(Claims.Role, role))
            {
                claimsIdentity.AddClaim(new Claim(Claims.Role, role).SetDestinations(Destinations.AccessToken, Destinations.IdentityToken));
            }
        }

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

    private (string Message, bool IsAssignmentRequired) ResolveAccessDenial(string? reason)
    {
        return reason switch
        {
            "This application is no longer available."
                => (_localizer["AuthorizationApplicationUnavailableReason"].Value, false),
            "This enterprise application is disabled. Contact an administrator to enable it."
                => (_localizer["AuthorizationApplicationDisabledReason"].Value, false),
            "Your tenant is not authorized for this application."
                => (_localizer["AuthorizationTenantNotAllowedReason"].Value, false),
            "Your account is valid, but it has not been assigned to this application."
                => (_localizer["AuthorizationAssignmentRequiredReason"].Value, true),
            _ => (reason ?? _localizer["AuthorizationAccessDeniedDefaultReason"].Value, false)
        };
    }

    private async Task<IActionResult> HandleAuthorizationDenialAsync(
        OpenIddictRequest request,
        object application,
        ApplicationUser user,
        string denialReason,
        bool isAssignmentRequired)
    {
        var applicationName = await _applicationManager.GetLocalizedDisplayNameAsync(application)
            ?? request.ClientId
            ?? _localizer["Application"].Value;

        _logger.LogWarning(
            "Authorization denied. ClientId: {ClientId}; UserId: {UserId}; Reason: {Reason}; CorrelationId: {CorrelationId}",
            request.ClientId,
            user.Id,
            denialReason,
            HttpContext.TraceIdentifier);

        // This is configured per client in Admin / Clients / Branding.
        // Disabled preserves the traditional OIDC behavior: return access_denied
        // to the requesting application immediately, without an intermediate page.
        if (!IsAuthorizationDeniedPageEnabled(application) ||
            (HttpMethods.IsPost(Request.Method) && Request.Form.ContainsKey("continue_denied")))
        {
            return CreateAccessDeniedResult(denialReason);
        }

        var returnUrl = Request.PathBase + Request.Path + Request.QueryString;

        return View("AccessDenied", new AuthorizationAccessDeniedViewModel
        {
            ApplicationName = applicationName,
            ClientId = request.ClientId ?? string.Empty,
            Reason = denialReason,
            CorrelationId = HttpContext.TraceIdentifier,
            ReturnUrl = returnUrl,
            IsAssignmentRequired = isAssignmentRequired
        });
    }


    private IActionResult CreateAccessDeniedResult(string denialReason)
    {
        return Forbid(
            new AuthenticationProperties(new Dictionary<string, string?>
            {
                [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.AccessDenied,
                [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = denialReason
            }),
            OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    private static bool IsAuthorizationDeniedPageEnabled(object application)
    {
        var properties = application.GetType().GetProperty("Properties")?.GetValue(application)?.ToString();
        if (string.IsNullOrWhiteSpace(properties))
        {
            return true;
        }

        try
        {
            using var document = JsonDocument.Parse(properties);
            if (!document.RootElement.TryGetProperty("auth2demo:branding", out var branding) ||
                branding.ValueKind != JsonValueKind.Object)
            {
                return true;
            }

            // Missing property means enabled to preserve the new professional screen
            // for clients created before this option existed.
            if (!branding.TryGetProperty("showAuthorizationDeniedPage", out var enabled))
            {
                return true;
            }

            return enabled.ValueKind != JsonValueKind.False;
        }
        catch (JsonException)
        {
            return true;
        }
    }


    private static string? GetMissingRequiredClaim(object application, ClaimsPrincipal user)
    {
        foreach (var requiredClaim in ExtractRequiredClaims(application))
        {
            if (!user.Claims.Any(claim =>
                    string.Equals(claim.Type, requiredClaim.Type, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(claim.Value, requiredClaim.Value, StringComparison.OrdinalIgnoreCase)))
            {
                return $"{requiredClaim.Type}={requiredClaim.Value}";
            }
        }

        return null;
    }

    private static IEnumerable<RequiredClaim> ExtractRequiredClaims(object application)
    {
        var properties = application.GetType().GetProperty("Properties")?.GetValue(application)?.ToString();
        if (string.IsNullOrWhiteSpace(properties))
        {
            return [];
        }

        try
        {
            using var document = JsonDocument.Parse(properties);
            if (!document.RootElement.TryGetProperty("auth2demo:required_claims", out var value))
            {
                return [];
            }

            return JsonSerializer.Deserialize<RequiredClaim[]>(value.GetRawText()) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private sealed record RequiredClaim(string Type, string Value);

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
