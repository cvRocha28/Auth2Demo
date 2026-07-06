using Auth2Demo.Domain.Identity;
using Auth2Demo.Infrastructure.Security;
using Auth2Demo.Web.Areas.Admin.Models;
using Auth2Demo.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System.Globalization;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;
using Auth2Demo.Web;

namespace Auth2Demo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = AuthPolicies.ClientManager)]
public sealed class ClientsController : Controller
{
    private static readonly string[] BuiltInScopes =
    [
        Scopes.OpenId,
        Scopes.Profile,
        Scopes.Email,
        Scopes.Roles,
        Scopes.OfflineAccess
    ];

    private const string RequiredClaimPermissionPrefix = "custom:required_claim:";
    private const string ClientSecretPermissionPrefix = "custom:client_secret:";

    private readonly IOpenIddictApplicationManager _applicationManager;
    private readonly IOpenIddictScopeManager _scopeManager;
    private readonly IClientSecretGenerator _secretGenerator;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public ClientsController(
        IOpenIddictApplicationManager applicationManager,
        IOpenIddictScopeManager scopeManager,
        IClientSecretGenerator secretGenerator,
        IStringLocalizer<SharedResource> localizer)
    {
        _applicationManager = applicationManager;
        _scopeManager = scopeManager;
        _secretGenerator = secretGenerator;
        _localizer = localizer;
    }

    public async Task<IActionResult> Index()
    {
        var clients = new List<ClientIndexViewModel>();

        await foreach (var app in _applicationManager.ListAsync(100, 0))
        {
            var permissions = await _applicationManager.GetPermissionsAsync(app);
            clients.Add(new ClientIndexViewModel
            {
                ClientId = await _applicationManager.GetClientIdAsync(app) ?? string.Empty,
                DisplayName = await _applicationManager.GetDisplayNameAsync(app) ?? string.Empty,
                ClientType = await _applicationManager.GetClientTypeAsync(app) ?? string.Empty,
                ConsentType = await _applicationManager.GetConsentTypeAsync(app) ?? string.Empty,
                GrantTypes = ExtractGrantTypes(permissions).ToArray()
            });
        }

        return View(clients.OrderBy(client => client.ClientId).ToArray());
    }

    public IActionResult Create()
    {
        return View(ClientFormViewModel.CreateDefault());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ClientFormViewModel model)
    {
        model.IsEdit = false;
        NormalizeModel(model);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (await _applicationManager.FindByClientIdAsync(model.ClientId) is not null)
        {
            ModelState.AddModelError(nameof(model.ClientId), _localizer["ClientIdAlreadyExists"].Value);
            return View(model);
        }

        var descriptor = BuildDescriptor(model);
        var secret = model.ClientType == ClientTypes.Confidential && model.GenerateSecret
            ? _secretGenerator.GenerateSecret()
            : null;

        if (!string.IsNullOrWhiteSpace(secret))
        {
            descriptor.ClientSecret = secret;
            AddSecretMetadata(descriptor, "Default secret");
        }

        await _applicationManager.CreateAsync(descriptor);

        if (!string.IsNullOrWhiteSpace(secret))
        {
            TempData["GeneratedSecret"] = secret;
        }

        TempData["Success"] = _localizer["ClientCreatedSuccessfully"].Value;
        return RedirectToAction(nameof(Details), new { clientId = model.ClientId });
    }

    public async Task<IActionResult> Edit(string clientId)
    {
        var app = await _applicationManager.FindByClientIdAsync(clientId);

        if (app is null)
        {
            return NotFound();
        }

        return View(await BuildFormViewModelAsync(app));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ClientFormViewModel model)
    {
        model.IsEdit = true;
        NormalizeModel(model);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var app = await _applicationManager.FindByClientIdAsync(model.OriginalClientId);

        if (app is null)
        {
            return NotFound();
        }

        if (!string.Equals(model.OriginalClientId, model.ClientId, StringComparison.Ordinal))
        {
            if (await _applicationManager.FindByClientIdAsync(model.ClientId) is not null)
            {
                ModelState.AddModelError(nameof(model.ClientId), _localizer["ClientIdAlreadyExists"].Value);
                return View(model);
            }
        }

        var descriptor = new OpenIddictApplicationDescriptor();
        await _applicationManager.PopulateAsync(descriptor, app);

        ApplyModelToDescriptor(model, descriptor);

        var generatedSecret = string.Empty;
        if (model.ClientType == ClientTypes.Confidential && model.RegenerateSecret)
        {
            generatedSecret = _secretGenerator.GenerateSecret();
            descriptor.ClientSecret = generatedSecret;
        }

        if (model.ClientType == ClientTypes.Public)
        {
            descriptor.ClientSecret = null;
        }

        await _applicationManager.UpdateAsync(app, descriptor);

        if (!string.IsNullOrWhiteSpace(generatedSecret))
        {
            TempData["GeneratedSecret"] = generatedSecret;
        }

        TempData["Success"] = _localizer["ClientUpdatedSuccessfully"].Value;
        return RedirectToAction(nameof(Details), new { clientId = model.ClientId });
    }

    public async Task<IActionResult> Details(string clientId)
    {
        var app = await _applicationManager.FindByClientIdAsync(clientId);

        if (app is null)
        {
            return NotFound();
        }

        return View(await BuildDetailsViewModelAsync(app));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateScopes(ClientScopesUpdateViewModel model)
    {
        var app = await _applicationManager.FindByClientIdAsync(model.ClientId);

        if (app is null)
        {
            return NotFound();
        }

        var descriptor = new OpenIddictApplicationDescriptor();
        await _applicationManager.PopulateAsync(descriptor, app);

        RemovePermissionsByPrefix(descriptor, Permissions.Prefixes.Scope);
        AddScopePermissions(descriptor, model.SelectedScopes ?? []);

        await _applicationManager.UpdateAsync(app, descriptor);

        TempData["Success"] = _localizer["AllowedScopesUpdatedSuccessfully"].Value;
        return RedirectToAction(nameof(Details), new { clientId = model.ClientId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegenerateSecret(string clientId)
    {
        var app = await _applicationManager.FindByClientIdAsync(clientId);

        if (app is null)
        {
            return NotFound();
        }

        var descriptor = new OpenIddictApplicationDescriptor();
        await _applicationManager.PopulateAsync(descriptor, app);

        descriptor.ClientType = ClientTypes.Confidential;
        descriptor.ClientSecret = _secretGenerator.GenerateSecret();

        await _applicationManager.UpdateAsync(app, descriptor);

        TempData["GeneratedSecret"] = descriptor.ClientSecret;
        TempData["Success"] = _localizer["ClientSecretRegenerated"].Value;

        return RedirectToAction(nameof(Details), new { clientId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddSecret(string clientId, string? displayName)
    {
        var app = await _applicationManager.FindByClientIdAsync(clientId);

        if (app is null)
        {
            return NotFound();
        }

        var descriptor = new OpenIddictApplicationDescriptor();
        await _applicationManager.PopulateAsync(descriptor, app);

        descriptor.ClientType = ClientTypes.Confidential;
        descriptor.ClientSecret = _secretGenerator.GenerateSecret();
        AddSecretMetadata(descriptor, string.IsNullOrWhiteSpace(displayName) ? "Client secret" : displayName.Trim());

        await _applicationManager.UpdateAsync(app, descriptor);

        TempData["GeneratedSecret"] = descriptor.ClientSecret;
        TempData["Success"] = "Secret created successfully. Copy it now because it will not be shown again.";

        return RedirectToAction(nameof(Details), new { clientId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string clientId)
    {
        var app = await _applicationManager.FindByClientIdAsync(clientId);

        if (app is not null)
        {
            await _applicationManager.DeleteAsync(app);
            TempData["Success"] = _localizer["ClientDeletedSuccessfully"].Value;
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task<ClientFormViewModel> BuildFormViewModelAsync(object app)
    {
        var permissions = await _applicationManager.GetPermissionsAsync(app);
        var requirements = await _applicationManager.GetRequirementsAsync(app);
        var clientType = await _applicationManager.GetClientTypeAsync(app) ?? ClientTypes.Confidential;
        var consentType = await _applicationManager.GetConsentTypeAsync(app) ?? ConsentTypes.Explicit;
        var redirectUris = await _applicationManager.GetRedirectUrisAsync(app);
        var postLogoutRedirectUris = await _applicationManager.GetPostLogoutRedirectUrisAsync(app);
        var clientId = await _applicationManager.GetClientIdAsync(app) ?? string.Empty;

        return new ClientFormViewModel
        {
            IsEdit = true,
            ClientId = clientId,
            OriginalClientId = clientId,
            DisplayName = await _applicationManager.GetDisplayNameAsync(app) ?? string.Empty,
            ClientType = clientType,
            ConsentType = consentType,
            Kind = InferKind(clientType, permissions),
            RedirectUris = string.Join(Environment.NewLine, redirectUris),
            PostLogoutRedirectUris = string.Join(Environment.NewLine, postLogoutRedirectUris),
            Scopes = string.Join(Environment.NewLine, ExtractScopes(permissions)),
            GrantTypes = string.Join(Environment.NewLine, ExtractGrantTypes(permissions)),
            RequiredClaims = string.Join(Environment.NewLine, ExtractRequiredClaims(permissions).Select(claim => string.IsNullOrWhiteSpace(claim.Value) ? claim.Type : $"{claim.Type}={claim.Value}")),
            AllowAuthorizationCode = permissions.Contains(Permissions.GrantTypes.AuthorizationCode),
            AllowRefreshToken = permissions.Contains(Permissions.GrantTypes.RefreshToken),
            AllowClientCredentials = permissions.Contains(Permissions.GrantTypes.ClientCredentials),
            RequirePkce = requirements.Contains(Requirements.Features.ProofKeyForCodeExchange),
            AllowAuthorizationEndpoint = permissions.Contains(Permissions.Endpoints.Authorization),
            AllowTokenEndpoint = permissions.Contains(Permissions.Endpoints.Token),
            AllowEndSessionEndpoint = permissions.Contains(Permissions.Endpoints.EndSession),
            AllowRevocationEndpoint = permissions.Contains(Permissions.Endpoints.Revocation),
            AllowIntrospectionEndpoint = permissions.Contains(Permissions.Endpoints.Introspection),
            GenerateSecret = false
        };
    }

    private async Task<ClientDetailsViewModel> BuildDetailsViewModelAsync(object app)
    {
        var permissions = await _applicationManager.GetPermissionsAsync(app);
        var requirements = await _applicationManager.GetRequirementsAsync(app);

        var allowedScopes = ExtractScopes(permissions).ToArray();
        var availableScopes = await BuildAvailableScopesAsync(allowedScopes);

        return new ClientDetailsViewModel
        {
            ClientId = await _applicationManager.GetClientIdAsync(app) ?? string.Empty,
            DisplayName = await _applicationManager.GetDisplayNameAsync(app) ?? string.Empty,
            ClientType = await _applicationManager.GetClientTypeAsync(app) ?? string.Empty,
            ConsentType = await _applicationManager.GetConsentTypeAsync(app) ?? string.Empty,
            RedirectUris = (await _applicationManager.GetRedirectUrisAsync(app)).ToArray(),
            PostLogoutRedirectUris = (await _applicationManager.GetPostLogoutRedirectUrisAsync(app)).ToArray(),
            AllowedScopes = allowedScopes,
            GrantTypes = ExtractGrantTypes(permissions).ToArray(),
            Secrets = ExtractSecrets(permissions).ToArray(),
            RequiredClaims = ExtractRequiredClaims(permissions).ToArray(),
            Endpoints = ExtractEndpoints(permissions).ToArray(),
            RequirePkce = requirements.Contains(Requirements.Features.ProofKeyForCodeExchange),
            AvailableScopes = availableScopes
        };
    }

    private async Task<IReadOnlyList<ClientScopeOptionViewModel>> BuildAvailableScopesAsync(IReadOnlyCollection<string> allowedScopes)
    {
        var availableScopes = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var scope in BuiltInScopes)
        {
            availableScopes[scope] = scope;
        }

        await foreach (var scope in _scopeManager.ListAsync(100, 0))
        {
            var name = await _scopeManager.GetNameAsync(scope);

            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            var displayName = await _scopeManager.GetDisplayNameAsync(scope);
            availableScopes[name] = string.IsNullOrWhiteSpace(displayName) ? name : displayName;
        }

        foreach (var scope in allowedScopes)
        {
            if (!availableScopes.ContainsKey(scope))
            {
                availableScopes[scope] = scope;
            }
        }

        return availableScopes
            .Select(scope => new ClientScopeOptionViewModel
            {
                Name = scope.Key,
                DisplayName = scope.Value,
                IsAllowed = allowedScopes.Contains(scope.Key, StringComparer.Ordinal)
            })
            .OrderBy(scope => GetScopeSortOrder(scope.Name))
            .ThenBy(scope => scope.Name)
            .ToArray();
    }

    private static OpenIddictApplicationDescriptor BuildDescriptor(ClientFormViewModel model)
    {
        var descriptor = new OpenIddictApplicationDescriptor();
        ApplyModelToDescriptor(model, descriptor);
        return descriptor;
    }

    private static void ApplyModelToDescriptor(ClientFormViewModel model, OpenIddictApplicationDescriptor descriptor)
    {
        descriptor.ClientId = model.ClientId.Trim();
        descriptor.DisplayName = model.DisplayName.Trim();
        descriptor.ClientType = model.ClientType;
        descriptor.ConsentType = model.ConsentType;

        var existingSecretPermissions = descriptor.Permissions
            .Where(permission => permission.StartsWith(ClientSecretPermissionPrefix, StringComparison.Ordinal))
            .ToArray();

        descriptor.RedirectUris.Clear();
        descriptor.PostLogoutRedirectUris.Clear();
        descriptor.Permissions.Clear();
        descriptor.Requirements.Clear();

        foreach (var permission in existingSecretPermissions)
        {
            descriptor.Permissions.Add(permission);
        }

        foreach (var uri in Split(model.RedirectUris))
        {
            descriptor.RedirectUris.Add(new Uri(uri, UriKind.Absolute));
        }

        foreach (var uri in Split(model.PostLogoutRedirectUris))
        {
            descriptor.PostLogoutRedirectUris.Add(new Uri(uri, UriKind.Absolute));
        }

        AddEndpointPermissions(descriptor, model);
        AddGrantTypePermissions(descriptor, model);
        AddScopePermissions(descriptor, Split(model.Scopes));
        AddRequiredClaimPermissions(descriptor, ParseRequiredClaims(model.RequiredClaims));

        if (model.RequirePkce && model.AllowAuthorizationCode)
        {
            descriptor.Requirements.Add(Requirements.Features.ProofKeyForCodeExchange);
        }
    }

    private static void AddEndpointPermissions(OpenIddictApplicationDescriptor descriptor, ClientFormViewModel model)
    {
        if (model.AllowAuthorizationEndpoint)
        {
            descriptor.Permissions.Add(Permissions.Endpoints.Authorization);
        }

        if (model.AllowTokenEndpoint)
        {
            descriptor.Permissions.Add(Permissions.Endpoints.Token);
        }

        if (model.AllowEndSessionEndpoint)
        {
            descriptor.Permissions.Add(Permissions.Endpoints.EndSession);
        }

        if (model.AllowRevocationEndpoint)
        {
            descriptor.Permissions.Add(Permissions.Endpoints.Revocation);
        }

        if (model.AllowIntrospectionEndpoint)
        {
            descriptor.Permissions.Add(Permissions.Endpoints.Introspection);
        }
    }

    private static void AddGrantTypePermissions(OpenIddictApplicationDescriptor descriptor, ClientFormViewModel model)
    {
        foreach (var grantType in Split(model.GrantTypes))
        {
            var normalizedGrantType = NormalizeGrantType(grantType);

            if (string.IsNullOrWhiteSpace(normalizedGrantType))
            {
                continue;
            }

            descriptor.Permissions.Add(Permissions.Prefixes.GrantType + normalizedGrantType);

            if (string.Equals(normalizedGrantType, GrantTypes.AuthorizationCode, StringComparison.Ordinal))
            {
                descriptor.Permissions.Add(Permissions.ResponseTypes.Code);
            }
        }
    }

    private static void AddScopePermissions(OpenIddictApplicationDescriptor descriptor, IEnumerable<string> scopes)
    {
        foreach (var scope in scopes
            .Where(scope => !string.IsNullOrWhiteSpace(scope))
            .Select(scope => scope.Trim())
            .Distinct(StringComparer.Ordinal))
        {
            descriptor.Permissions.Add(Permissions.Prefixes.Scope + scope);
        }
    }

    private static void AddRequiredClaimPermissions(OpenIddictApplicationDescriptor descriptor, IEnumerable<ClientRequiredClaimViewModel> claims)
    {
        foreach (var claim in claims
            .Where(claim => !string.IsNullOrWhiteSpace(claim.Type))
            .DistinctBy(claim => claim.Type + "\u001f" + claim.Value))
        {
            descriptor.Permissions.Add(RequiredClaimPermissionPrefix + EncodePart(claim.Type) + "=" + EncodePart(claim.Value));
        }
    }

    private static void AddSecretMetadata(OpenIddictApplicationDescriptor descriptor, string displayName)
    {
        descriptor.Permissions.Add(ClientSecretPermissionPrefix + Guid.NewGuid().ToString("N") + "|" + EncodePart(displayName) + "|" + DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
    }

    private static void RemovePermissionsByPrefix(OpenIddictApplicationDescriptor descriptor, string prefix)
    {
        foreach (var permission in descriptor.Permissions
            .Where(permission => permission.StartsWith(prefix, StringComparison.Ordinal))
            .ToArray())
        {
            descriptor.Permissions.Remove(permission);
        }
    }

    private static IEnumerable<ClientSecretViewModel> ExtractSecrets(IEnumerable<string> permissions)
    {
        return permissions
            .Where(permission => permission.StartsWith(ClientSecretPermissionPrefix, StringComparison.Ordinal))
            .Select(permission => permission[ClientSecretPermissionPrefix.Length..].Split('|'))
            .Where(parts => parts.Length >= 3)
            .Select(parts => new ClientSecretViewModel
            {
                Id = parts[0],
                DisplayName = DecodePart(parts[1]),
                CreatedAtUtc = DateTime.TryParse(parts[2], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var createdAt)
                    ? createdAt
                    : DateTime.MinValue
            })
            .OrderByDescending(secret => secret.CreatedAtUtc);
    }

    private static IEnumerable<ClientRequiredClaimViewModel> ExtractRequiredClaims(IEnumerable<string> permissions)
    {
        foreach (var permission in permissions.Where(permission => permission.StartsWith(RequiredClaimPermissionPrefix, StringComparison.Ordinal)))
        {
            var value = permission[RequiredClaimPermissionPrefix.Length..];
            var separator = value.IndexOf('=');

            if (separator < 0)
            {
                yield return new ClientRequiredClaimViewModel { Type = DecodePart(value) };
                continue;
            }

            yield return new ClientRequiredClaimViewModel
            {
                Type = DecodePart(value[..separator]),
                Value = DecodePart(value[(separator + 1)..])
            };
        }
    }

    private static IEnumerable<string> ExtractScopes(IEnumerable<string> permissions)
    {
        return permissions
            .Where(permission => permission.StartsWith(Permissions.Prefixes.Scope, StringComparison.Ordinal))
            .Select(permission => permission[Permissions.Prefixes.Scope.Length..])
            .Distinct(StringComparer.Ordinal)
            .OrderBy(scope => GetScopeSortOrder(scope))
            .ThenBy(scope => scope);
    }

    private static IEnumerable<string> ExtractGrantTypes(IEnumerable<string> permissions)
    {
        return permissions
            .Where(permission => permission.StartsWith(Permissions.Prefixes.GrantType, StringComparison.Ordinal))
            .Select(permission => permission[Permissions.Prefixes.GrantType.Length..])
            .Distinct(StringComparer.Ordinal)
            .OrderBy(permission => permission);
    }

    private static IEnumerable<string> ExtractEndpoints(IEnumerable<string> permissions)
    {
        return permissions
            .Where(permission => permission.StartsWith(Permissions.Prefixes.Endpoint, StringComparison.Ordinal))
            .Select(permission => permission[Permissions.Prefixes.Endpoint.Length..])
            .Distinct(StringComparer.Ordinal)
            .OrderBy(permission => permission);
    }

    private static ClientKind InferKind(string clientType, IEnumerable<string> permissions)
    {
        if (permissions.Contains(Permissions.GrantTypes.ClientCredentials) &&
            !permissions.Contains(Permissions.GrantTypes.AuthorizationCode))
        {
            return ClientKind.MachineToMachine;
        }

        if (string.Equals(clientType, ClientTypes.Public, StringComparison.Ordinal))
        {
            return ClientKind.Spa;
        }

        return ClientKind.WebApplication;
    }

    private static IEnumerable<ClientRequiredClaimViewModel> ParseRequiredClaims(string? value)
    {
        foreach (var item in (value ?? string.Empty).Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var separator = item.IndexOf('=');
            if (separator < 0)
            {
                yield return new ClientRequiredClaimViewModel { Type = item.Trim(), Value = string.Empty };
                continue;
            }

            yield return new ClientRequiredClaimViewModel
            {
                Type = item[..separator].Trim(),
                Value = item[(separator + 1)..].Trim()
            };
        }
    }

    private static string NormalizeGrantType(string value)
    {
        var normalized = value.Trim().Replace("_", "", StringComparison.Ordinal).ToLowerInvariant();

        return normalized switch
        {
            "authorizationcode" => GrantTypes.AuthorizationCode,
            "authorization_code" => GrantTypes.AuthorizationCode,
            "clientcredentials" => GrantTypes.ClientCredentials,
            "client_credentials" => GrantTypes.ClientCredentials,
            "refreshtoken" => GrantTypes.RefreshToken,
            "refresh_token" => GrantTypes.RefreshToken,
            _ => normalized
        };
    }

    private static string EncodePart(string value) => Uri.EscapeDataString(value ?? string.Empty);

    private static string DecodePart(string value) => Uri.UnescapeDataString(value ?? string.Empty);

    private static int GetScopeSortOrder(string scope)
    {
        return scope switch
        {
            Scopes.OpenId => 0,
            Scopes.Profile => 1,
            Scopes.Email => 2,
            Scopes.Roles => 3,
            Scopes.OfflineAccess => 4,
            _ => 100
        };
    }

    private static IEnumerable<string> Split(string? value)
    {
        return (value ?? string.Empty)
            .Split([' ', ',', ';', '\r', '\n', '\t'],
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static void NormalizeModel(ClientFormViewModel model)
    {
        model.ClientId = model.ClientId.Trim();
        model.OriginalClientId = string.IsNullOrWhiteSpace(model.OriginalClientId)
            ? model.ClientId
            : model.OriginalClientId.Trim();
        model.DisplayName = model.DisplayName.Trim();

        if (model.Kind is ClientKind.Spa or ClientKind.NativeApplication)
        {
            model.ClientType = ClientTypes.Public;
            model.GenerateSecret = false;
            model.RegenerateSecret = false;
        }
        else if (string.IsNullOrWhiteSpace(model.ClientType))
        {
            model.ClientType = ClientTypes.Confidential;
        }

        if (model.Kind == ClientKind.MachineToMachine)
        {
            model.AllowAuthorizationCode = false;
            model.AllowRefreshToken = false;
            model.AllowClientCredentials = true;
            model.AllowAuthorizationEndpoint = false;
            model.AllowEndSessionEndpoint = false;
            model.RequirePkce = false;
            model.RedirectUris = string.Empty;
            model.PostLogoutRedirectUris = string.Empty;
        }

        if (string.IsNullOrWhiteSpace(model.GrantTypes))
        {
            var grantTypes = new List<string>();

            if (model.AllowAuthorizationCode)
            {
                grantTypes.Add(GrantTypes.AuthorizationCode);
            }

            if (model.AllowRefreshToken)
            {
                grantTypes.Add(GrantTypes.RefreshToken);
            }

            if (model.AllowClientCredentials)
            {
                grantTypes.Add(GrantTypes.ClientCredentials);
            }

            model.GrantTypes = string.Join(Environment.NewLine, grantTypes);
        }

        var normalizedGrantTypes = Split(model.GrantTypes).Select(NormalizeGrantType).ToArray();
        model.AllowAuthorizationCode = normalizedGrantTypes.Contains(GrantTypes.AuthorizationCode, StringComparer.Ordinal);
        model.AllowRefreshToken = normalizedGrantTypes.Contains(GrantTypes.RefreshToken, StringComparer.Ordinal);
        model.AllowClientCredentials = normalizedGrantTypes.Contains(GrantTypes.ClientCredentials, StringComparer.Ordinal);

        if (!model.AllowAuthorizationCode)
        {
            model.RequirePkce = false;
        }

        if (model.AllowAuthorizationCode)
        {
            model.AllowAuthorizationEndpoint = true;
        }

        if (model.AllowAuthorizationCode || model.AllowRefreshToken || model.AllowClientCredentials)
        {
            model.AllowTokenEndpoint = true;
        }
    }
}
