using Auth2Demo.Domain.Identity;
using Auth2Demo.Infrastructure.Security;
using Auth2Demo.Web.Areas.Admin.Models;
using Auth2Demo.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
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

        descriptor.RedirectUris.Clear();
        descriptor.PostLogoutRedirectUris.Clear();
        descriptor.Permissions.Clear();
        descriptor.Requirements.Clear();

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
        if (model.AllowAuthorizationCode)
        {
            descriptor.Permissions.Add(Permissions.GrantTypes.AuthorizationCode);
            descriptor.Permissions.Add(Permissions.ResponseTypes.Code);
        }

        if (model.AllowRefreshToken)
        {
            descriptor.Permissions.Add(Permissions.GrantTypes.RefreshToken);
        }

        if (model.AllowClientCredentials)
        {
            descriptor.Permissions.Add(Permissions.GrantTypes.ClientCredentials);
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

    private static void RemovePermissionsByPrefix(OpenIddictApplicationDescriptor descriptor, string prefix)
    {
        foreach (var permission in descriptor.Permissions
            .Where(permission => permission.StartsWith(prefix, StringComparison.Ordinal))
            .ToArray())
        {
            descriptor.Permissions.Remove(permission);
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
