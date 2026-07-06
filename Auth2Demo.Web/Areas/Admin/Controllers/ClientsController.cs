using Auth2Demo.Application.Services.Admin;
using Auth2Demo.Web.Areas.Admin.Models;
using Auth2Demo.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;
using Auth2Demo.Web;
using System.Text.Json;

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
    private readonly IClientApplicationAdminService _clientApplications;
    private readonly IAdminIdentityProviderService _identityProviders;

    public ClientsController(
        IOpenIddictApplicationManager applicationManager,
        IOpenIddictScopeManager scopeManager,
        IClientSecretGenerator secretGenerator,
        IStringLocalizer<SharedResource> localizer,
        IClientApplicationAdminService clientApplications,
        IAdminIdentityProviderService identityProviders)
    {
        _applicationManager = applicationManager;
        _scopeManager = scopeManager;
        _secretGenerator = secretGenerator;
        _localizer = localizer;
        _clientApplications = clientApplications;
        _identityProviders = identityProviders;
    }

    private Task<bool> ActiveClientIdExistsAsync(string clientId, Guid? ignoreApplicationId = null)
    {
        return _clientApplications.ActiveClientIdExistsAsync(clientId, ignoreApplicationId);
    }

    private async Task<object?> FindActiveClientByClientIdAsync(string clientId)
    {
        var app = await _applicationManager.FindByClientIdAsync(clientId);

        if (app is null)
        {
            return null;
        }

        var metadata = await GetApplicationAuditMetadataAsync(app);
        return metadata.IsDeleted ? null : app;
    }

    public async Task<IActionResult> Index()
    {
        var clients = new List<ClientIndexViewModel>();

        await foreach (var app in _applicationManager.ListAsync(100, 0))
        {
            var permissions = await _applicationManager.GetPermissionsAsync(app);
            var metadata = await GetApplicationAuditMetadataAsync(app);
            if (metadata.IsDeleted)
            {
                continue;
            }

            clients.Add(new ClientIndexViewModel
            {
                ClientId = await _applicationManager.GetClientIdAsync(app) ?? string.Empty,
                DisplayName = await _applicationManager.GetDisplayNameAsync(app) ?? string.Empty,
                ClientType = await _applicationManager.GetClientTypeAsync(app) ?? string.Empty,
                ConsentType = await _applicationManager.GetConsentTypeAsync(app) ?? string.Empty,
                GrantTypes = ExtractGrantTypes(permissions).ToArray(),
                CreatedAt = metadata.CreatedAt,
                UpdatedAt = metadata.UpdatedAt,
                IsEnabled = metadata.IsEnabled,
                BrandingEnabled = ExtractClientBranding(app)?.IsEnabled == true
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

        if (await ActiveClientIdExistsAsync(model.ClientId))
        {
            ModelState.AddModelError(nameof(model.ClientId), _localizer["ClientIdAlreadyExists"].Value);
            return View(model);
        }

        var descriptor = BuildDescriptor(model);
        var generatedSecrets = new List<GeneratedClientSecret>();
        var initialSecret = BuildPrimarySecretForOpenIddict(model.SecretItems);

        if (string.Equals(model.ClientType, ClientTypes.Confidential, StringComparison.Ordinal))
        {
            if (initialSecret is null)
            {
                var fallbackSecret = _secretGenerator.GenerateSecret();
                initialSecret = new GeneratedClientSecret(_localizer["DefaultSecretDescription"].Value, fallbackSecret);
            }

            descriptor.ClientSecret = initialSecret.Secret;
        }
        else
        {
            descriptor.ClientSecret = null;
        }

        await _applicationManager.CreateAsync(descriptor);

        var createdApp = await _applicationManager.FindByClientIdAsync(model.ClientId)
            ?? throw new InvalidOperationException(_localizer["ClientCreatedButCouldNotBeLoaded"].Value);

        await MarkApplicationCreatedAsync(createdApp, "ClientCreated", model.ClientId);

        if (initialSecret is not null)
        {
            var expiresAtUtc = GetExpirationForPrimarySecret(model.SecretItems) ?? CalculateExpiration(ClientSecretExpiration.Never);
            await CreateClientSecretAsync(createdApp, initialSecret.Description, initialSecret.Secret, expiresAtUtc);
            generatedSecrets.Add(initialSecret);
        }

        if (generatedSecrets.Count > 0)
        {
            TempData["GeneratedSecret"] = string.Join(Environment.NewLine, generatedSecrets.Select(secret => $"{secret.Description}: {secret.Secret}"));
        }

        TempData["Success"] = _localizer["ClientCreatedSuccessfully"].Value;
        return RedirectToAction(nameof(Details), new { clientId = model.ClientId });
    }

    public async Task<IActionResult> Edit(string clientId)
    {
        var app = await FindActiveClientByClientIdAsync(clientId);

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

        var app = await FindActiveClientByClientIdAsync(model.OriginalClientId);

        if (app is null)
        {
            return NotFound();
        }

        var currentApplicationId = await GetApplicationGuidAsync(app);

        if (!string.Equals(model.OriginalClientId, model.ClientId, StringComparison.Ordinal))
        {
            if (await ActiveClientIdExistsAsync(model.ClientId, currentApplicationId))
            {
                ModelState.AddModelError(nameof(model.ClientId), _localizer["ClientIdAlreadyExists"].Value);
                return View(model);
            }
        }

        var descriptor = new OpenIddictApplicationDescriptor();
        await _applicationManager.PopulateAsync(descriptor, app);

        ApplyModelToDescriptor(model, descriptor);

        var generatedSecrets = new List<GeneratedClientSecret>();
        var changedToPublic = model.ClientType == ClientTypes.Public;
        var newPrimarySecret = changedToPublic
            ? null
            : BuildPrimarySecretForOpenIddict(model.SecretItems.Where(secret => !secret.IsExisting || !string.IsNullOrWhiteSpace(secret.PlainTextSecret)));

        if (changedToPublic)
        {
            descriptor.ClientSecret = null;
        }
        else if (newPrimarySecret is not null)
        {
            descriptor.ClientSecret = newPrimarySecret.Secret;
        }

        await _applicationManager.UpdateAsync(app, descriptor);

        if (changedToPublic)
        {
            await RevokeAllClientSecretsAsync(app, _localizer["ClientChangedToPublicReason"].Value);
        }
        else if (newPrimarySecret is not null)
        {
            var expiresAtUtc = GetExpirationForPrimarySecret(model.SecretItems) ?? CalculateExpiration(ClientSecretExpiration.Never);
            await CreateClientSecretAsync(app, newPrimarySecret.Description, newPrimarySecret.Secret, expiresAtUtc);
            generatedSecrets.Add(newPrimarySecret);
        }

        await MarkApplicationUpdatedAsync(app, "ClientUpdated", model.ClientId);

        if (generatedSecrets.Count > 0)
        {
            TempData["GeneratedSecret"] = string.Join(Environment.NewLine, generatedSecrets.Select(secret => $"{secret.Description}: {secret.Secret}"));
        }

        TempData["Success"] = _localizer["ClientUpdatedSuccessfully"].Value;
        return RedirectToAction(nameof(Details), new { clientId = model.ClientId });
    }

    public async Task<IActionResult> Details(string clientId)
    {
        var app = await FindActiveClientByClientIdAsync(clientId);

        if (app is null)
        {
            return NotFound();
        }

        return View(await BuildDetailsViewModelAsync(app));
    }

    public async Task<IActionResult> Branding(string clientId)
    {
        var app = await FindActiveClientByClientIdAsync(clientId);

        if (app is null)
        {
            return NotFound();
        }

        return View(await BuildClientBrandingViewModelAsync(app));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Branding(ClientBrandingViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.ProviderOptions = await BuildBrandingProviderOptionsAsync(model.EnabledProviderSchemes);
            return View(model);
        }

        var app = await FindActiveClientByClientIdAsync(model.ClientId);

        if (app is null)
        {
            return NotFound();
        }

        var descriptor = new OpenIddictApplicationDescriptor();
        await _applicationManager.PopulateAsync(descriptor, app);
        SetClientBrandingProperties(descriptor, model);
        await _applicationManager.UpdateAsync(app, descriptor);
        await MarkApplicationUpdatedAsync(app, "ClientBrandingUpdated", model.ClientId);

        TempData["Success"] = _localizer["ClientUpdatedSuccessfully"].Value;
        return RedirectToAction(nameof(Branding), new { clientId = model.ClientId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateScopes(ClientScopesUpdateViewModel model)
    {
        var app = await FindActiveClientByClientIdAsync(model.ClientId);

        if (app is null)
        {
            return NotFound();
        }

        var descriptor = new OpenIddictApplicationDescriptor();
        await _applicationManager.PopulateAsync(descriptor, app);

        RemovePermissionsByPrefix(descriptor, Permissions.Prefixes.Scope);
        AddScopePermissions(descriptor, model.SelectedScopes ?? []);

        await _applicationManager.UpdateAsync(app, descriptor);
        await MarkApplicationUpdatedAsync(app, "ClientScopesUpdated", model.ClientId);

        TempData["Success"] = _localizer["AllowedScopesUpdatedSuccessfully"].Value;
        return RedirectToAction(nameof(Details), new { clientId = model.ClientId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddSecret(string clientId, string? displayName, ClientSecretExpiration expiration = ClientSecretExpiration.Never)
    {
        var app = await FindActiveClientByClientIdAsync(clientId);

        if (app is null)
        {
            return NotFound();
        }

        var clientType = await _applicationManager.GetClientTypeAsync(app);
        if (!string.Equals(clientType, ClientTypes.Confidential, StringComparison.Ordinal))
        {
            TempData["Success"] = _localizer["OnlyConfidentialClientsCanHaveSecrets"].Value;
            return RedirectToAction(nameof(Details), new { clientId });
        }

        var secret = _secretGenerator.GenerateSecret();
        var description = string.IsNullOrWhiteSpace(displayName) ? _localizer["NewSecretDefaultDescription"].Value : displayName.Trim();
        var expiresAtUtc = CalculateExpiration(expiration);

        var descriptor = new OpenIddictApplicationDescriptor();
        await _applicationManager.PopulateAsync(descriptor, app);
        descriptor.ClientSecret = secret;
        await _applicationManager.UpdateAsync(app, descriptor);

        await CreateClientSecretAsync(app, description, secret, expiresAtUtc);
        await MarkApplicationUpdatedAsync(app, "ClientSecretAdded", clientId);

        TempData["GeneratedSecret"] = $"{description}: {secret}";
        TempData["Success"] = _localizer["ClientSecretAddedSuccessfully"].Value;
        return RedirectToAction(nameof(Details), new { clientId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RevokeSecret(string clientId, Guid secretId)
    {
        var app = await FindActiveClientByClientIdAsync(clientId);

        if (app is null)
        {
            return NotFound();
        }

        await RevokeClientSecretAsync(app, secretId, _localizer["RevokedByAdmin"].Value);
        await MarkApplicationUpdatedAsync(app, "ClientSecretRevoked", clientId);
        TempData["Success"] = _localizer["ClientSecretRevokedSuccessfully"].Value;

        return RedirectToAction(nameof(Details), new { clientId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string clientId)
    {
        var app = await FindActiveClientByClientIdAsync(clientId);

        if (app is not null)
        {
            await SoftDeleteApplicationAsync(app, clientId);
            await RevokeAllClientSecretsAsync(app, _localizer["ClientDeletedSuccessfully"].Value);
            TempData["Success"] = _localizer["ClientDeletedSuccessfully"].Value;
        }

        return RedirectToAction(nameof(Index));
    }

    private async Task MarkApplicationCreatedAsync(object app, string eventType, string clientId)
    {
        var applicationId = await GetApplicationGuidAsync(app);
        await _clientApplications.MarkCreatedAsync(applicationId, eventType, clientId, BuildAuditActor());
    }

    private async Task MarkApplicationUpdatedAsync(object app, string eventType, string clientId)
    {
        var applicationId = await GetApplicationGuidAsync(app);
        await _clientApplications.MarkUpdatedAsync(applicationId, eventType, clientId, BuildAuditActor());
    }

    private async Task SoftDeleteApplicationAsync(object app, string clientId)
    {
        var applicationId = await GetApplicationGuidAsync(app);
        await _clientApplications.SoftDeleteAsync(applicationId, clientId, BuildAuditActor());
    }

    private Guid? GetCurrentUserId()
    {
        var value = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(value, out var id) ? id : null;
    }

    private AdminAuditActor BuildAuditActor()
    {
        return new AdminAuditActor(
            GetCurrentUserId(),
            User.Identity?.Name,
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString());
    }

    private async Task<ApplicationAuditMetadata> GetApplicationAuditMetadataAsync(object app)
    {
        var applicationId = await GetApplicationGuidAsync(app);
        return await _clientApplications.GetMetadataAsync(applicationId);
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
            RedirectUriItems = redirectUris.Select(uri => uri.ToString()).ToList(),
            PostLogoutRedirectUriItems = postLogoutRedirectUris.Select(uri => uri.ToString()).ToList(),
            ScopeItems = ExtractScopes(permissions).ToList(),
            GrantTypeItems = ExtractGrantTypes(permissions).ToList(),
            SecretItems = await BuildSecretInputItemsAsync(app),
            RequiredClaimItems = ExtractRequiredClaims(app).ToList(),
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

        var metadata = await GetApplicationAuditMetadataAsync(app);

        return new ClientDetailsViewModel
        {
            ClientId = await _applicationManager.GetClientIdAsync(app) ?? string.Empty,
            DisplayName = await _applicationManager.GetDisplayNameAsync(app) ?? string.Empty,
            ClientType = await _applicationManager.GetClientTypeAsync(app) ?? string.Empty,
            ConsentType = await _applicationManager.GetConsentTypeAsync(app) ?? string.Empty,
            CreatedAt = metadata.CreatedAt,
            UpdatedAt = metadata.UpdatedAt,
            IsEnabled = metadata.IsEnabled,
            RedirectUris = (await _applicationManager.GetRedirectUrisAsync(app)).ToArray(),
            PostLogoutRedirectUris = (await _applicationManager.GetPostLogoutRedirectUrisAsync(app)).ToArray(),
            AllowedScopes = allowedScopes,
            GrantTypes = ExtractGrantTypes(permissions).ToArray(),
            Endpoints = ExtractEndpoints(permissions).ToArray(),
            RequirePkce = requirements.Contains(Requirements.Features.ProofKeyForCodeExchange),
            AvailableScopes = availableScopes,
            Secrets = await BuildSecretViewModelsAsync(app),
            RequiredClaims = ExtractRequiredClaims(app).ToArray(),
            Branding = ExtractClientBranding(app)
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
        SetRequiredClaimProperties(descriptor, model.RequiredClaimItems);

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


    private sealed record GeneratedClientSecret(string Description, string Secret);

    private GeneratedClientSecret? BuildPrimarySecretForOpenIddict(IEnumerable<ClientSecretInputViewModel> secretItems)
    {
        var item = secretItems
            .Where(secret => !secret.Remove)
            .Where(secret => !secret.IsExisting || !string.IsNullOrWhiteSpace(secret.PlainTextSecret))
            .LastOrDefault();

        if (item is null)
        {
            return null;
        }

        var plainSecret = string.IsNullOrWhiteSpace(item.PlainTextSecret)
            ? _secretGenerator.GenerateSecret()
            : item.PlainTextSecret.Trim();

        var description = string.IsNullOrWhiteSpace(item.Description)
            ? _localizer["DefaultSecretDescription"].Value
            : item.Description.Trim();

        return new GeneratedClientSecret(description, plainSecret);
    }

    private static DateTimeOffset? GetExpirationForPrimarySecret(IEnumerable<ClientSecretInputViewModel> secretItems)
    {
        var item = secretItems
            .Where(secret => !secret.Remove)
            .Where(secret => !secret.IsExisting || !string.IsNullOrWhiteSpace(secret.PlainTextSecret))
            .LastOrDefault();

        return item?.ExpiresAtUtc ?? (item is null ? null : CalculateExpiration(item.Expiration));
    }

    private async Task<List<GeneratedClientSecret>> SaveClientSecretsAsync(
        object app,
        IEnumerable<ClientSecretInputViewModel> secretItems)
    {
        var generatedSecrets = new List<GeneratedClientSecret>();

        var submittedSecretIds = secretItems
            .Where(secret => !secret.Remove && secret.Id.HasValue)
            .Select(secret => secret.Id!.Value)
            .ToHashSet();

        foreach (var secretItem in secretItems.Where(secret => secret.Remove && secret.Id.HasValue))
        {
            await RevokeClientSecretAsync(app, secretItem.Id.Value, _localizer["RemovedByAdminReason"].Value);
        }

        await RevokeSecretsMissingFromFormAsync(app, submittedSecretIds);

        foreach (var secretItem in secretItems.Where(secret => !secret.Remove))
        {
            if (secretItem.IsExisting && string.IsNullOrWhiteSpace(secretItem.PlainTextSecret))
            {
                continue;
            }

            var plainSecret = string.IsNullOrWhiteSpace(secretItem.PlainTextSecret)
                ? _secretGenerator.GenerateSecret()
                : secretItem.PlainTextSecret.Trim();

            var description = string.IsNullOrWhiteSpace(secretItem.Description)
                ? _localizer["DefaultSecretDescription"].Value
                : secretItem.Description.Trim();

            var expiresAtUtc = secretItem.ExpiresAtUtc ?? CalculateExpiration(secretItem.Expiration);
            await CreateClientSecretAsync(app, description, plainSecret, expiresAtUtc);
            generatedSecrets.Add(new GeneratedClientSecret(description, plainSecret));
        }


        return generatedSecrets;
    }

    private async Task CreateClientSecretAsync(object app, string description, string plainSecret, DateTimeOffset? expiresAtUtc)
    {
        var applicationId = await GetApplicationGuidAsync(app);
        await _clientApplications.CreateSecretAsync(applicationId, description, plainSecret, expiresAtUtc);
    }


    private async Task RevokeSecretsMissingFromFormAsync(object app, HashSet<Guid> submittedSecretIds)
    {
        var applicationId = await GetApplicationGuidAsync(app);
        await _clientApplications.RevokeSecretsMissingFromFormAsync(
            applicationId,
            submittedSecretIds,
            _localizer["RemovedFromClientFormReason"].Value);
    }

    private async Task RevokeClientSecretAsync(object app, Guid secretId, string reason)
    {
        var applicationId = await GetApplicationGuidAsync(app);
        await _clientApplications.RevokeSecretAsync(applicationId, secretId, reason);
    }

    private async Task RevokeAllClientSecretsAsync(object app, string reason)
    {
        var applicationId = await GetApplicationGuidAsync(app);
        await _clientApplications.RevokeAllSecretsAsync(applicationId, reason);
    }

    private async Task<List<ClientSecretInputViewModel>> BuildSecretInputItemsAsync(object app)
    {
        var applicationId = await GetApplicationGuidAsync(app);
        var secrets = await _clientApplications.ListActiveSecretsAsync(applicationId);

        return secrets.Select(secret => new ClientSecretInputViewModel
        {
            Id = secret.Id,
            Description = secret.Description,
            IsExisting = true,
            CreatedAt = secret.CreatedAtUtc.ToString("O"),
            ExpiresAtUtc = secret.ExpiresAtUtc,
            RevokedAtUtc = secret.RevokedAtUtc,
            Expiration = ClientSecretExpiration.Never
        }).ToList();
    }

    private async Task<ClientSecretViewModel[]> BuildSecretViewModelsAsync(object app)
    {
        var applicationId = await GetApplicationGuidAsync(app);
        var secrets = await _clientApplications.ListAllSecretsAsync(applicationId);

        return secrets.Select(secret => new ClientSecretViewModel
        {
            Id = secret.Id,
            Description = secret.Description,
            SecretPrefix = secret.SecretPrefix,
            CreatedAtUtc = secret.CreatedAtUtc,
            ExpiresAtUtc = secret.ExpiresAtUtc,
            RevokedAtUtc = secret.RevokedAtUtc
        }).ToArray();
    }

    private async Task<Guid> GetApplicationGuidAsync(object app)
    {
        var id = await _applicationManager.GetIdAsync(app);
        if (Guid.TryParse(id, out var applicationId))
        {
            return applicationId;
        }

        throw new InvalidOperationException(_localizer["ApplicationIdInvalid"].Value);
    }

    private static DateTimeOffset? CalculateExpiration(ClientSecretExpiration expiration)
    {
        return expiration == ClientSecretExpiration.Never
            ? null
            : DateTimeOffset.UtcNow.AddMonths((int)expiration);
    }

    private async Task<ClientBrandingViewModel> BuildClientBrandingViewModelAsync(object app)
    {
        var clientId = await _applicationManager.GetClientIdAsync(app) ?? string.Empty;
        var branding = ExtractClientBranding(app) ?? new ClientBrandingViewModel();

        branding.ClientId = clientId;
        branding.DisplayName = await _applicationManager.GetDisplayNameAsync(app) ?? string.Empty;

        ApplyClientBrandingDefaults(branding);
        branding.ProviderOptions = await BuildBrandingProviderOptionsAsync(branding.EnabledProviderSchemes);

        return branding;
    }

    private async Task<List<ClientBrandingProviderOptionViewModel>> BuildBrandingProviderOptionsAsync(IEnumerable<string> enabledSchemes)
    {
        var enabled = enabledSchemes
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var providers = await _identityProviders.ListAsync();
        return providers
            .Where(provider => provider.IsEnabled)
            .OrderBy(provider => provider.SortOrder)
            .ThenBy(provider => provider.DisplayName)
            .Select(provider => new ClientBrandingProviderOptionViewModel
            {
                Scheme = provider.Scheme,
                DisplayName = provider.DisplayName,
                ButtonText = string.IsNullOrWhiteSpace(provider.DisplayName) ? provider.Scheme : provider.DisplayName,
                IsConfigured = provider.HasClientId && provider.HasClientSecret,
                SortOrder = provider.SortOrder
            })
            .ToList();
    }

    private static void ApplyClientBrandingDefaults(ClientBrandingViewModel branding)
    {
        branding.PrimaryColor = NormalizeColor(branding.PrimaryColor, "#2563eb");
        branding.SecondaryColor = NormalizeColor(branding.SecondaryColor, "#0f172a");
        branding.BackgroundColor = NormalizeColor(branding.BackgroundColor, "#eff6ff");
        branding.SurfaceColor = NormalizeColor(branding.SurfaceColor, "#ffffff");
        branding.TextColor = NormalizeColor(branding.TextColor, "#111827");
        branding.MutedTextColor = NormalizeColor(branding.MutedTextColor, "#64748b");
        branding.BorderColor = NormalizeColor(branding.BorderColor, "#cbd5e1");
        branding.SuccessColor = NormalizeColor(branding.SuccessColor, "#16a34a");
        branding.WarningColor = NormalizeColor(branding.WarningColor, "#f59e0b");
        branding.DangerColor = NormalizeColor(branding.DangerColor, "#dc2626");
        branding.CardRadius = Math.Clamp(branding.CardRadius == 0 ? 28 : branding.CardRadius, 8, 48);
        branding.ButtonRadius = Math.Clamp(branding.ButtonRadius == 0 ? 14 : branding.ButtonRadius, 6, 32);
        branding.EnableLocalLogin = branding.EnableLocalLogin;
        branding.EnabledProviderSchemes = branding.EnabledProviderSchemes
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (string.IsNullOrWhiteSpace(branding.Theme))
        {
            branding.Theme = "Light";
        }
    }

    private static void SetClientBrandingProperties(OpenIddictApplicationDescriptor descriptor, ClientBrandingViewModel model)
    {
        var branding = new ClientBrandingViewModel
        {
            IsEnabled = model.IsEnabled,
            TenantName = NormalizeNullable(model.TenantName),
            LogoUrl = NormalizeNullable(model.LogoUrl),
            FaviconUrl = NormalizeNullable(model.FaviconUrl),
            PrimaryColor = NormalizeColor(model.PrimaryColor, "#2563eb"),
            SecondaryColor = NormalizeColor(model.SecondaryColor, "#0f172a"),
            BackgroundColor = NormalizeColor(model.BackgroundColor, "#eff6ff"),
            SurfaceColor = NormalizeColor(model.SurfaceColor, "#ffffff"),
            TextColor = NormalizeColor(model.TextColor, "#111827"),
            MutedTextColor = NormalizeColor(model.MutedTextColor, "#64748b"),
            BorderColor = NormalizeColor(model.BorderColor, "#cbd5e1"),
            SuccessColor = NormalizeColor(model.SuccessColor, "#16a34a"),
            WarningColor = NormalizeColor(model.WarningColor, "#f59e0b"),
            DangerColor = NormalizeColor(model.DangerColor, "#dc2626"),
            CardRadius = Math.Clamp(model.CardRadius, 8, 48),
            ButtonRadius = Math.Clamp(model.ButtonRadius, 6, 32),
            UseGradientButtons = model.UseGradientButtons,
            ShowCreateAccountLink = model.ShowCreateAccountLink,
            ShowForgotPasswordLink = model.ShowForgotPasswordLink,
            EnableLocalLogin = model.EnableLocalLogin,
            EnabledProviderSchemes = model.EnabledProviderSchemes
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList(),
            Theme = string.Equals(model.Theme, "Dark", StringComparison.OrdinalIgnoreCase) ? "Dark" : "Light",
            CustomCss = NormalizeNullable(model.CustomCss)
        };

        descriptor.Properties["auth2demo:branding"] = JsonSerializer.SerializeToElement(branding, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }

    private static ClientBrandingViewModel? ExtractClientBranding(object appOrDescriptor)
    {
        var json = TryGetPropertyJson(appOrDescriptor, "auth2demo:branding");
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<ClientBrandingViewModel>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        }
        catch
        {
            return null;
        }
    }

    private static string? NormalizeNullable(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string NormalizeColor(string? value, string fallback) =>
        !string.IsNullOrWhiteSpace(value) && value.Trim().StartsWith('#') ? value.Trim() : fallback;

    private static void SetRequiredClaimProperties(OpenIddictApplicationDescriptor descriptor, IEnumerable<ClientRequiredClaimInputViewModel> claims)
    {
        var items = claims
            .Where(claim => !claim.Remove)
            .Where(claim => !string.IsNullOrWhiteSpace(claim.Type) && !string.IsNullOrWhiteSpace(claim.Value))
            .Select(claim => new ClientRequiredClaimInputViewModel
            {
                Type = claim.Type.Trim(),
                Value = claim.Value.Trim()
            })
            .ToArray();

        descriptor.Properties["auth2demo:required_claims"] = JsonSerializer.SerializeToElement(items);
    }

    private static IEnumerable<ClientRequiredClaimInputViewModel> ExtractRequiredClaims(object appOrDescriptor)
    {
        var json = TryGetPropertyJson(appOrDescriptor, "auth2demo:required_claims");
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<ClientRequiredClaimInputViewModel[]>(json) ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static string? TryGetPropertyJson(object appOrDescriptor, string name)
    {
        if (appOrDescriptor is OpenIddictApplicationDescriptor descriptor && descriptor.Properties.TryGetValue(name, out var descriptorValue))
        {
            return descriptorValue.GetRawText();
        }

        var property = appOrDescriptor.GetType().GetProperty("Properties")?.GetValue(appOrDescriptor);
        return property switch
        {
            string value => TryExtractPropertyJson(value, name),
            null => null,
            _ => TryExtractPropertyJson(property.ToString(), name)
        };
    }

    private static string? TryExtractPropertyJson(string? propertiesJson, string name)
    {
        if (string.IsNullOrWhiteSpace(propertiesJson))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(propertiesJson);
            return document.RootElement.TryGetProperty(name, out var value) ? value.GetRawText() : null;
        }
        catch
        {
            return null;
        }
    }

    private static void NormalizeModel(ClientFormViewModel model)
    {
        model.ClientId = model.ClientId.Trim();
        model.OriginalClientId = string.IsNullOrWhiteSpace(model.OriginalClientId)
            ? model.ClientId
            : model.OriginalClientId.Trim();
        model.DisplayName = model.DisplayName.Trim();
        model.RedirectUris = string.Join(Environment.NewLine, model.RedirectUriItems.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim()));
        model.PostLogoutRedirectUris = string.Join(Environment.NewLine, model.PostLogoutRedirectUriItems.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim()));
        model.Scopes = string.Join(Environment.NewLine, model.ScopeItems.Where(value => !string.IsNullOrWhiteSpace(value)).Select(value => value.Trim()));
        model.AllowAuthorizationCode = model.GrantTypeItems.Contains(GrantTypes.AuthorizationCode, StringComparer.Ordinal);
        model.AllowRefreshToken = model.GrantTypeItems.Contains(GrantTypes.RefreshToken, StringComparer.Ordinal);
        model.AllowClientCredentials = model.GrantTypeItems.Contains(GrantTypes.ClientCredentials, StringComparer.Ordinal);

        if (model.RedirectUriItems.Count == 0 && !string.IsNullOrWhiteSpace(model.RedirectUris))
        {
            model.RedirectUriItems = Split(model.RedirectUris).ToList();
        }

        if (model.PostLogoutRedirectUriItems.Count == 0 && !string.IsNullOrWhiteSpace(model.PostLogoutRedirectUris))
        {
            model.PostLogoutRedirectUriItems = Split(model.PostLogoutRedirectUris).ToList();
        }

        if (model.ScopeItems.Count == 0 && !string.IsNullOrWhiteSpace(model.Scopes))
        {
            model.ScopeItems = Split(model.Scopes).ToList();
        }

        if (model.Kind is ClientKind.Spa or ClientKind.NativeApplication)
        {
            model.ClientType = ClientTypes.Public;
            model.GenerateSecret = false;
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
