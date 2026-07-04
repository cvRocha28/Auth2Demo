using Auth2Demo.Application.Services.Admin;
using Auth2Demo.Domain.Identity;
using Auth2Demo.Domain.Security;
using Auth2Demo.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;

namespace Auth2Demo.Infrastructure.Services.Admin;

public interface IAdminAuditLogService
{
    Task<IReadOnlyList<AuditLog>> SearchAsync(string? q, string? category);
    Task<IReadOnlyList<string>> GetCategoriesAsync();
}

public sealed class AdminAuditLogService : IAdminAuditLogService
{
    private readonly IAdminAuditLogRepository _auditLogs;

    public AdminAuditLogService(IAdminAuditLogRepository auditLogs)
    {
        _auditLogs = auditLogs;
    }

    public Task<IReadOnlyList<AuditLog>> SearchAsync(string? q, string? category)
    {
        return _auditLogs.SearchAsync(q, category);
    }

    public Task<IReadOnlyList<string>> GetCategoriesAsync()
    {
        return _auditLogs.GetCategoriesAsync();
    }
}

public interface IAdminBrandingService
{
    Task<BrandingSettings> GetAsync();
    Task SaveAsync(BrandingSettings model);
}

public sealed class AdminBrandingService : IAdminBrandingService
{
    private readonly IAdminBrandingRepository _branding;

    public AdminBrandingService(IAdminBrandingRepository branding)
    {
        _branding = branding;
    }

    public async Task<BrandingSettings> GetAsync()
    {
        return await _branding.GetAsync()
            ?? new BrandingSettings
            {
                Id = Guid.NewGuid()
            };
    }

    public Task SaveAsync(BrandingSettings model)
    {
        return _branding.SaveAsync(model);
    }
}

public interface IAdminDashboardService
{
    Task<AdminDashboardData> GetAsync();
}

public sealed class AdminDashboardService : IAdminDashboardService
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly IOpenIddictApplicationManager _apps;
    private readonly IOpenIddictScopeManager _scopes;
    private readonly IAdminAuditLogRepository _auditLogs;
    private readonly IAdminDashboardRepository _dashboard;

    public AdminDashboardService(
        UserManager<ApplicationUser> users,
        IOpenIddictApplicationManager apps,
        IOpenIddictScopeManager scopes,
        IAdminAuditLogRepository auditLogs,
        IAdminDashboardRepository dashboard)
    {
        _users = users;
        _apps = apps;
        _scopes = scopes;
        _auditLogs = auditLogs;
        _dashboard = dashboard;
    }

    public async Task<AdminDashboardData> GetAsync()
    {
        var today = DateTimeOffset.UtcNow.Date;

        return new AdminDashboardData(
            await _users.Users.CountAsync(),
            await _users.Users.CountAsync(x => x.CreatedAt >= today),
            await _apps.CountAsync(),
            await _scopes.CountAsync(),
            await _dashboard.CountEnabledIdentityProvidersAsync(),
            await _auditLogs.CountLoginEventsFromAsync(today),
            await _auditLogs.CountFailedLoginEventsFromAsync(today),
            await _dashboard.CountActiveSessionsAsync(),
            await _dashboard.CountTrustedDevicesAsync(),
            await _auditLogs.GetRecentAsync(8),
            await _auditLogs.GetProviderUsageAsync(6));
    }
}

public interface IAdminDeviceService
{
    Task<IReadOnlyList<UserDevice>> ListAsync();
    Task ToggleTrustedAsync(Guid id);
}

public sealed class AdminDeviceService : IAdminDeviceService
{
    private readonly IAdminDeviceRepository _devices;

    public AdminDeviceService(IAdminDeviceRepository devices)
    {
        _devices = devices;
    }

    public Task<IReadOnlyList<UserDevice>> ListAsync()
    {
        return _devices.ListAsync(200);
    }

    public Task ToggleTrustedAsync(Guid id)
    {
        return _devices.ToggleTrustedAsync(id);
    }
}

public interface IAdminEmailTemplateService
{
    Task<IReadOnlyList<EmailTemplate>> ListAsync();
    Task<EmailTemplate> GetForEditAsync(Guid id);
    Task SaveAsync(EmailTemplate model);
}

public sealed class AdminEmailTemplateService : IAdminEmailTemplateService
{
    private readonly IAdminEmailTemplateRepository _emailTemplates;

    public AdminEmailTemplateService(IAdminEmailTemplateRepository emailTemplates)
    {
        _emailTemplates = emailTemplates;
    }

    public async Task<IReadOnlyList<EmailTemplate>> ListAsync()
    {
        return await _emailTemplates.ListAsync();
    }

    public async Task<EmailTemplate> GetForEditAsync(Guid id)
    {
        return await _emailTemplates.GetAsync(id)
            ?? new EmailTemplate
            {
                Id = Guid.NewGuid(),
                Key = "custom",
                Name = "Novo template"
            };
    }

    public Task SaveAsync(EmailTemplate model)
    {
        return _emailTemplates.SaveAsync(model);
    }
}

public interface IAdminHealthService
{
    Task<AdminHealthData> GetAsync();
}

public sealed class AdminHealthService : IAdminHealthService
{
    private readonly IAdminHealthRepository _health;
    private readonly IAdminAuditLogRepository _auditLogs;

    public AdminHealthService(
        IAdminHealthRepository health,
        IAdminAuditLogRepository auditLogs)
    {
        _health = health;
        _auditLogs = auditLogs;
    }

    public async Task<AdminHealthData> GetAsync()
    {
        return new AdminHealthData(
            await _health.CanConnectDatabaseAsync(),
            await _auditLogs.GetLastAuditAtAsync(),
            await _health.CountEnabledIdentityProvidersAsync());
    }
}

public interface IAdminSecuritySettingsService
{
    Task<SecuritySettings> GetAsync();
    Task SaveAsync(SecuritySettings model);
}

public sealed class AdminSecuritySettingsService : IAdminSecuritySettingsService
{
    private readonly IAdminSecuritySettingsRepository _settings;

    public AdminSecuritySettingsService(IAdminSecuritySettingsRepository settings)
    {
        _settings = settings;
    }

    public async Task<SecuritySettings> GetAsync()
    {
        return await _settings.GetAsync()
            ?? new SecuritySettings
            {
                Id = Guid.NewGuid()
            };
    }

    public Task SaveAsync(SecuritySettings model)
    {
        return _settings.SaveAsync(model);
    }
}

public interface IAdminSessionService
{
    Task<IReadOnlyList<UserSession>> ListAsync();
    Task RevokeAsync(Guid id);
}

public sealed class AdminSessionService : IAdminSessionService
{
    private readonly IAdminSessionRepository _sessions;

    public AdminSessionService(IAdminSessionRepository sessions)
    {
        _sessions = sessions;
    }

    public Task<IReadOnlyList<UserSession>> ListAsync()
    {
        return _sessions.ListAsync(200);
    }

    public Task RevokeAsync(Guid id)
    {
        return _sessions.RevokeAsync(id);
    }
}

public interface IAdminPasskeyService
{
    Task<IReadOnlyList<PasskeyCredential>> ListAsync();
}

public sealed class AdminPasskeyService : IAdminPasskeyService
{
    private readonly IAdminPasskeyRepository _passkeys;

    public AdminPasskeyService(IAdminPasskeyRepository passkeys)
    {
        _passkeys = passkeys;
    }

    public Task<IReadOnlyList<PasskeyCredential>> ListAsync()
    {
        return _passkeys.ListAsync();
    }
}

public interface IAdminPermissionService
{
    Task<AdminPermissionIndexData> GetIndexAsync();
    Task CreateAsync(string name, string displayName, string category, string? description);
    Task ToggleAsync(Guid roleId, Guid permissionId);
}

public sealed class AdminPermissionService : IAdminPermissionService
{
    private readonly IAdminPermissionRepository _permissions;
    private readonly RoleManager<ApplicationRole> _roles;

    public AdminPermissionService(
        IAdminPermissionRepository permissions,
        RoleManager<ApplicationRole> roles)
    {
        _permissions = permissions;
        _roles = roles;
    }

    public async Task<AdminPermissionIndexData> GetIndexAsync()
    {
        return new AdminPermissionIndexData(
            await _roles.Roles
                .OrderBy(x => x.Name)
                .Select(x => new AdminPermissionRoleData(x.Id, x.Name))
                .ToListAsync(),
            await _permissions.GetRolePermissionsAsync(),
            await _permissions.GetPermissionsAsync());
    }

    public async Task CreateAsync(string name, string displayName, string category, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        var cleanName = name.Trim();

        if (await _permissions.PermissionExistsAsync(cleanName))
        {
            return;
        }

        await _permissions.CreateAsync(new Permission
        {
            Id = Guid.NewGuid(),
            Name = cleanName,
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? cleanName : displayName.Trim(),
            Category = string.IsNullOrWhiteSpace(category) ? "General" : category.Trim(),
            Description = description
        });
    }

    public Task ToggleAsync(Guid roleId, Guid permissionId)
    {
        return _permissions.ToggleAsync(roleId, permissionId);
    }
}

public interface IAdminRoleService
{
    Task<IReadOnlyList<ApplicationRole>> ListAsync();
    Task<(bool Success, string Message)> CreateAsync(string name, string? description);
    Task<(bool NotFound, bool Success, string Message)> DeleteAsync(Guid id);
}

public sealed class AdminRoleService : IAdminRoleService
{
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminRoleService(
        RoleManager<ApplicationRole> roleManager,
        UserManager<ApplicationUser> userManager)
    {
        _roleManager = roleManager;
        _userManager = userManager;
    }

    public async Task<IReadOnlyList<ApplicationRole>> ListAsync()
    {
        return await _roleManager.Roles
            .OrderBy(x => x.Name)
            .ToListAsync();
    }

    public async Task<(bool Success, string Message)> CreateAsync(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return (false, "RoleNameRequired");
        }

        name = name.Trim();

        if (await _roleManager.RoleExistsAsync(name))
        {
            return (false, "RoleAlreadyExists");
        }

        var result = await _roleManager.CreateAsync(new ApplicationRole
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description?.Trim()
        });

        return result.Succeeded
            ? (true, "RoleCreatedSuccessfully")
            : (false, string.Join("; ", result.Errors.Select(x => x.Description)));
    }

    public async Task<(bool NotFound, bool Success, string Message)> DeleteAsync(Guid id)
    {
        var role = await _roleManager.FindByIdAsync(id.ToString());

        if (role is null)
        {
            return (true, false, string.Empty);
        }

        if (role.IsSystemRole)
        {
            return (false, false, "SystemRolesCannotBeDeleted");
        }

        var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name!);

        if (usersInRole.Count > 0)
        {
            return (false, false, "RemoveUsersFromRoleBeforeDeleting");
        }

        var result = await _roleManager.DeleteAsync(role);

        return result.Succeeded
            ? (false, true, "RoleDeletedSuccessfully")
            : (false, false, string.Join("; ", result.Errors.Select(x => x.Description)));
    }
}

public interface IAdminUserService
{
    Task<IReadOnlyList<ApplicationUser>> SearchAsync(string? q);
    Task<IReadOnlyList<string>> GetRoleNamesAsync();
    Task<(bool Success, IEnumerable<IdentityError> Errors)> CreateAsync(AdminUserCreateData model);
    Task<AdminUserEditData?> GetForEditAsync(Guid id);
    Task<(bool NotFound, bool Success, IEnumerable<IdentityError> Errors)> UpdateAsync(AdminUserEditData model);
    Task<bool> ToggleBlockAsync(Guid id);
}

public sealed class AdminUserService : IAdminUserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;

    public AdminUserService(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<IReadOnlyList<ApplicationUser>> SearchAsync(string? q)
    {
        var query = _userManager.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(q))
        {
            query = query.Where(x =>
                x.Email!.Contains(q) ||
                x.DisplayName.Contains(q));
        }

        return await query
            .OrderByDescending(x => x.CreatedAt)
            .Take(100)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<string>> GetRoleNamesAsync()
    {
        return await _roleManager.Roles
            .Select(x => x.Name!)
            .ToListAsync();
    }

    public async Task<(bool Success, IEnumerable<IdentityError> Errors)> CreateAsync(AdminUserCreateData model)
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = model.Email,
            Email = model.Email,
            DisplayName = model.DisplayName,
            EmailConfirmed = model.EmailConfirmed,
            Status = UserStatus.Active
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (!result.Succeeded)
        {
            return (false, result.Errors);
        }

        if (model.Roles.Length == 0)
        {
            return (true, Array.Empty<IdentityError>());
        }

        var rolesResult = await _userManager.AddToRolesAsync(
            user,
            model.Roles.Distinct(StringComparer.OrdinalIgnoreCase));

        if (rolesResult.Succeeded)
        {
            return (true, Array.Empty<IdentityError>());
        }

        await _userManager.DeleteAsync(user);
        return (false, rolesResult.Errors);
    }

    public async Task<AdminUserEditData?> GetForEditAsync(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());

        if (user is null)
        {
            return null;
        }

        var roles = await _userManager.GetRolesAsync(user);

        return new AdminUserEditData
        {
            Id = user.Id,
            DisplayName = user.DisplayName,
            Email = user.Email!,
            Status = user.Status,
            EmailConfirmed = user.EmailConfirmed,
            Roles = roles.ToArray()
        };
    }

    public async Task<(bool NotFound, bool Success, IEnumerable<IdentityError> Errors)> UpdateAsync(AdminUserEditData model)
    {
        var user = await _userManager.FindByIdAsync(model.Id.ToString());

        if (user is null)
        {
            return (true, false, Array.Empty<IdentityError>());
        }

        user.DisplayName = model.DisplayName;
        user.Email = model.Email;
        user.UserName = model.Email;
        user.Status = model.Status;
        user.EmailConfirmed = model.EmailConfirmed;
        user.UpdatedAt = DateTimeOffset.UtcNow;

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            return (false, false, result.Errors);
        }

        var current = await _userManager.GetRolesAsync(user);
        var selected = model.Roles.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var rolesToRemove = current.Except(selected, StringComparer.OrdinalIgnoreCase).ToArray();
        var rolesToAdd = selected.Except(current, StringComparer.OrdinalIgnoreCase).ToArray();

        if (rolesToRemove.Length > 0)
        {
            var removeResult = await _userManager.RemoveFromRolesAsync(user, rolesToRemove);

            if (!removeResult.Succeeded)
            {
                return (false, false, removeResult.Errors);
            }
        }

        if (rolesToAdd.Length > 0)
        {
            var addResult = await _userManager.AddToRolesAsync(user, rolesToAdd);

            if (!addResult.Succeeded)
            {
                return (false, false, addResult.Errors);
            }
        }

        return (false, true, Array.Empty<IdentityError>());
    }

    public async Task<bool> ToggleBlockAsync(Guid id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());

        if (user is null)
        {
            return false;
        }

        user.Status = user.Status == UserStatus.Blocked
            ? UserStatus.Active
            : UserStatus.Blocked;

        user.UpdatedAt = DateTimeOffset.UtcNow;
        await _userManager.UpdateAsync(user);

        return true;
    }
}

public interface IAdminIdentityProviderService
{
    Task<IReadOnlyList<IdentityProviderListItemData>> ListAsync();
    Task<IdentityProviderEditData?> GetForEditAsync(Guid id);
    Task<SaveIdentityProviderResult> SaveAsync(IdentityProviderEditData model);
    Task<(bool NotFound, string Message)> ToggleAsync(Guid id);
    Task<DeleteIdentityProviderResult> DeleteAsync(Guid id);
}

public sealed class AdminIdentityProviderService : IAdminIdentityProviderService
{
    private readonly IAdminIdentityProviderRepository _identityProviders;

    public AdminIdentityProviderService(IAdminIdentityProviderRepository identityProviders)
    {
        _identityProviders = identityProviders;
    }

    public Task<IReadOnlyList<IdentityProviderListItemData>> ListAsync()
    {
        return _identityProviders.ListAsync();
    }

    public async Task<IdentityProviderEditData?> GetForEditAsync(Guid id)
    {
        var provider = await _identityProviders.GetAsync(id, tracking: false);

        if (provider is null)
        {
            return null;
        }

        return new IdentityProviderEditData
        {
            Id = provider.Id,
            Name = provider.Name,
            DisplayName = provider.DisplayName,
            Scheme = provider.Scheme,
            Kind = provider.Kind,
            IconCssClass = provider.IconCssClass,
            ButtonText = provider.ButtonText,
            ClientId = provider.ClientId,
            ClientSecret = provider.ClientSecret,
            Authority = provider.Authority,
            CallbackPath = provider.CallbackPath,
            IsEnabled = provider.IsEnabled,
            IsSystemProvider = provider.IsSystemProvider,
            SortOrder = provider.SortOrder
        };
    }

    public async Task<SaveIdentityProviderResult> SaveAsync(IdentityProviderEditData model)
    {
        var normalizedName = model.Name.Trim();
        var normalizedScheme = model.Scheme.Trim();
        var currentId = model.Id ?? Guid.Empty;

        if (await _identityProviders.HasDuplicateAsync(currentId, normalizedName, normalizedScheme))
        {
            return new SaveIdentityProviderResult(false, true, false);
        }

        if (model.Id is null || model.Id == Guid.Empty)
        {
            var provider = new IdentityProvider(
                normalizedName,
                model.DisplayName,
                normalizedScheme,
                model.Kind);

            provider.Update(
                model.DisplayName,
                normalizedScheme,
                model.Kind,
                model.IconCssClass,
                model.ButtonText,
                model.ClientId,
                model.ClientSecret,
                model.Authority,
                model.CallbackPath,
                model.IsEnabled,
                model.SortOrder);

            await _identityProviders.AddAsync(provider);

            return new SaveIdentityProviderResult(false, false, true);
        }

        var current = await _identityProviders.GetAsync(model.Id.Value, tracking: true);

        if (current is null)
        {
            return new SaveIdentityProviderResult(true, false, false);
        }

        current.Update(
            model.DisplayName,
            normalizedScheme,
            model.Kind,
            model.IconCssClass,
            model.ButtonText,
            model.ClientId,
            model.ClientSecret,
            model.Authority,
            model.CallbackPath,
            model.IsEnabled,
            model.SortOrder);

        await _identityProviders.SaveChangesAsync();

        return new SaveIdentityProviderResult(false, false, false);
    }

    public async Task<(bool NotFound, string Message)> ToggleAsync(Guid id)
    {
        var provider = await _identityProviders.GetAsync(id, tracking: true);

        if (provider is null)
        {
            return (true, string.Empty);
        }

        provider.Update(
            provider.DisplayName,
            provider.Scheme,
            provider.Kind,
            provider.IconCssClass,
            provider.ButtonText,
            provider.ClientId,
            provider.ClientSecret,
            provider.Authority,
            provider.CallbackPath,
            !provider.IsEnabled,
            provider.SortOrder);

        await _identityProviders.SaveChangesAsync();

        return (false, provider.IsEnabled ? "Provider habilitado." : "Provider desabilitado.");
    }

    public async Task<DeleteIdentityProviderResult> DeleteAsync(Guid id)
    {
        var provider = await _identityProviders.GetAsync(id, tracking: true);

        if (provider is null)
        {
            return new DeleteIdentityProviderResult(true, false);
        }

        if (provider.IsSystemProvider)
        {
            return new DeleteIdentityProviderResult(false, true);
        }

        await _identityProviders.DeleteAsync(provider);

        return new DeleteIdentityProviderResult(false, false);
    }
}

public interface IAdminMfaService
{
    Task<AdminMfaIndexData> GetIndexAsync();
    Task<AdminMfaDetailsData?> GetDetailsAsync(Guid userId);
    Task SetMethodEnabledAsync(Guid userId, string method, bool enabled);
    Task UpsertMethodAsync(ApplicationUser user, string method, string displayName, bool enabled, bool isDefault, DateTimeOffset? lastUsedAt);
    Task<(bool Found, string MethodName)> DeleteMethodAsync(Guid userId, Guid methodId);
    Task AuditAsync(ApplicationUser user, string eventType, string description, string? ipAddress, string? userAgent);
}

public sealed class AdminMfaService : IAdminMfaService
{
    private readonly IAdminMfaRepository _mfa;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminMfaService(
        IAdminMfaRepository mfa,
        UserManager<ApplicationUser> userManager)
    {
        _mfa = mfa;
        _userManager = userManager;
    }

    public async Task<AdminMfaIndexData> GetIndexAsync()
    {
        var users = await _userManager.Users
            .AsNoTracking()
            .OrderBy(x => x.Email)
            .ToListAsync();

        var methods = await _mfa.ListMethodsAsync();
        var rows = new List<AdminMfaUserRowData>();

        foreach (var user in users)
        {
            var userMethods = methods.Where(x => x.UserId == user.Id).ToList();

            rows.Add(new AdminMfaUserRowData
            {
                UserId = user.Id,
                Email = user.Email ?? user.UserName ?? string.Empty,
                DisplayName = user.DisplayName,
                Status = user.Status,
                TwoFactorEnabled = user.TwoFactorEnabled,
                RecoveryCodesLeft = await _userManager.CountRecoveryCodesAsync(user),
                LastLoginAt = user.LastLoginAt,
                LastMfaUsedAt = userMethods
                    .Where(x => x.LastUsedAt.HasValue)
                    .OrderByDescending(x => x.LastUsedAt)
                    .Select(x => x.LastUsedAt)
                    .FirstOrDefault(),
                Methods = userMethods
                    .Where(x => x.IsEnabled)
                    .Select(x => x.DisplayName)
                    .ToArray()
            });
        }

        return new AdminMfaIndexData
        {
            Users = rows,
            TotalUsers = rows.Count,
            EnabledUsers = rows.Count(x => x.TwoFactorEnabled),
            WithoutMfaUsers = rows.Count(x => !x.TwoFactorEnabled)
        };
    }

    public async Task<AdminMfaDetailsData?> GetDetailsAsync(Guid userId)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            return null;
        }

        return new AdminMfaDetailsData
        {
            UserId = user.Id,
            Email = user.Email ?? user.UserName ?? string.Empty,
            DisplayName = user.DisplayName,
            Status = user.Status,
            TwoFactorEnabled = await _userManager.GetTwoFactorEnabledAsync(user),
            RecoveryCodesLeft = await _userManager.CountRecoveryCodesAsync(user),
            HasAuthenticator = !string.IsNullOrWhiteSpace(await _userManager.GetAuthenticatorKeyAsync(user)),
            Methods = await _mfa.ListMethodsByUserAsync(user.Id)
        };
    }

    public async Task SetMethodEnabledAsync(Guid userId, string method, bool enabled)
    {
        var entry = await _mfa.GetMethodByNameAsync(userId, method);

        if (entry is null)
        {
            return;
        }

        entry.IsEnabled = enabled;
        await _mfa.UpsertMethodAsync(entry);
    }

    public async Task UpsertMethodAsync(
        ApplicationUser user,
        string method,
        string displayName,
        bool enabled,
        bool isDefault,
        DateTimeOffset? lastUsedAt)
    {
        await _mfa.UpsertMethodAsync(new MfaMethod
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            UserEmail = user.Email ?? user.UserName ?? string.Empty,
            Method = method,
            DisplayName = displayName,
            IsEnabled = enabled,
            IsDefault = isDefault,
            LastUsedAt = lastUsedAt
        });
    }

    public async Task<(bool Found, string MethodName)> DeleteMethodAsync(Guid userId, Guid methodId)
    {
        var method = await _mfa.GetMethodByIdAsync(userId, methodId);

        if (method is null)
        {
            return (false, string.Empty);
        }

        var methodName = method.Method;
        await _mfa.DeleteMethodAsync(method);

        return (true, methodName);
    }

    public async Task AuditAsync(
        ApplicationUser user,
        string eventType,
        string description,
        string? ipAddress,
        string? userAgent)
    {
        await _mfa.AddAuditAsync(new AuditLog
        {
            Id = Guid.NewGuid(),
            EventType = eventType,
            Category = "Security",
            Outcome = "Success",
            UserId = user.Id,
            UserEmail = user.Email,
            Provider = "MFA",
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Description = description
        });
    }
}
