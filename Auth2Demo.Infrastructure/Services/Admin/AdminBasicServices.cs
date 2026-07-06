using Auth2Demo.Application.Services.Admin;
using Auth2Demo.Domain.Identity;
using Auth2Demo.Domain.Security;
using Auth2Demo.Infrastructure.Identity;
using Auth2Demo.Infrastructure.Repositories.Admin;
using Microsoft.AspNetCore.Identity;

namespace Auth2Demo.Infrastructure.Services.Admin;


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


public sealed class AdminDashboardService : IAdminDashboardService
{
    private readonly IAdminUserRepository _users;
    private readonly IAdminOpenIddictMetricsRepository _openIddictMetrics;
    private readonly IAdminAuditLogRepository _auditLogs;
    private readonly IAdminDashboardRepository _dashboard;

    public AdminDashboardService(
        IAdminUserRepository users,
        IAdminOpenIddictMetricsRepository openIddictMetrics,
        IAdminAuditLogRepository auditLogs,
        IAdminDashboardRepository dashboard)
    {
        _users = users;
        _openIddictMetrics = openIddictMetrics;
        _auditLogs = auditLogs;
        _dashboard = dashboard;
    }

    public async Task<AdminDashboardData> GetAsync()
    {
        var today = DateTimeOffset.UtcNow.Date;

        return new AdminDashboardData(
            await _users.CountAsync(),
            await _users.CountCreatedFromAsync(today),
            (int)await _openIddictMetrics.CountApplicationsAsync(),
            (int)await _openIddictMetrics.CountScopesAsync(),
            await _dashboard.CountEnabledIdentityProvidersAsync(),
            await _auditLogs.CountLoginEventsFromAsync(today),
            await _auditLogs.CountFailedLoginEventsFromAsync(today),
            await _dashboard.CountActiveSessionsAsync(),
            await _dashboard.CountTrustedDevicesAsync(),
            await _auditLogs.GetRecentAsync(8),
            await _auditLogs.GetProviderUsageAsync(6));
    }
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


public sealed class AdminPermissionService : IAdminPermissionService
{
    private readonly IAdminPermissionRepository _permissions;
    private readonly IAdminRoleRepository _roles;

    public AdminPermissionService(
        IAdminPermissionRepository permissions,
        IAdminRoleRepository roles)
    {
        _permissions = permissions;
        _roles = roles;
    }

    public async Task<AdminPermissionIndexData> GetIndexAsync()
    {
        return new AdminPermissionIndexData(
            await _roles.ListPermissionRolesAsync(),
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
    private readonly IAdminRoleRepository _roles;
    private readonly IAdminUserRepository _users;

    public AdminRoleService(
        IAdminRoleRepository roles,
        IAdminUserRepository users)
    {
        _roles = roles;
        _users = users;
    }

    public async Task<IReadOnlyList<ApplicationRole>> ListAsync()
    {
        return await _roles.ListAsync();
    }

    public async Task<(bool Success, string Message)> CreateAsync(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return (false, "RoleNameRequired");
        }

        name = name.Trim();

        if (await _roles.ExistsAsync(name))
        {
            return (false, "RoleAlreadyExists");
        }

        var result = await _roles.CreateAsync(new ApplicationRole
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description?.Trim()
        });

        return result.Success
            ? (true, "RoleCreatedSuccessfully")
            : (false, string.Join("; ", result.Errors.Select(x => x.Description)));
    }

    public async Task<(bool NotFound, bool Success, string Message)> DeleteAsync(Guid id)
    {
        var role = await _roles.FindByIdAsync(id);

        if (role is null)
        {
            return (true, false, string.Empty);
        }

        if (role.IsSystemRole)
        {
            return (false, false, "SystemRolesCannotBeDeleted");
        }

        var usersInRole = await _users.GetUsersInRoleAsync(role.Name!);

        if (usersInRole.Count > 0)
        {
            return (false, false, "RemoveUsersFromRoleBeforeDeleting");
        }

        var result = await _roles.DeleteAsync(role);

        return result.Success
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
    private readonly IAdminUserRepository _users;
    private readonly IAdminRoleRepository _roles;

    public AdminUserService(
        IAdminUserRepository users,
        IAdminRoleRepository roles)
    {
        _users = users;
        _roles = roles;
    }

    public Task<IReadOnlyList<ApplicationUser>> SearchAsync(string? q)
    {
        return _users.SearchAsync(q, 100);
    }

    public Task<IReadOnlyList<string>> GetRoleNamesAsync()
    {
        return _roles.ListNamesAsync();
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

        var result = await _users.CreateAsync(user, model.Password);

        if (!result.Success)
        {
            return (false, result.Errors);
        }

        if (model.Roles.Length == 0)
        {
            return (true, Array.Empty<IdentityError>());
        }

        var rolesResult = await _users.AddToRolesAsync(
            user,
            model.Roles.Distinct(StringComparer.OrdinalIgnoreCase));

        if (rolesResult.Success)
        {
            return (true, Array.Empty<IdentityError>());
        }

        await _users.DeleteAsync(user);
        return (false, rolesResult.Errors);
    }

    public async Task<AdminUserEditData?> GetForEditAsync(Guid id)
    {
        var user = await _users.FindByIdAsync(id);

        if (user is null)
        {
            return null;
        }

        var roles = await _users.GetRolesAsync(user);

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
        var user = await _users.FindByIdAsync(model.Id);

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

        var result = await _users.UpdateAsync(user);

        if (!result.Success)
        {
            return (false, false, result.Errors);
        }

        var current = await _users.GetRolesAsync(user);
        var selected = model.Roles.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var rolesToRemove = current.Except(selected, StringComparer.OrdinalIgnoreCase).ToArray();
        var rolesToAdd = selected.Except(current, StringComparer.OrdinalIgnoreCase).ToArray();

        if (rolesToRemove.Length > 0)
        {
            var removeResult = await _users.RemoveFromRolesAsync(user, rolesToRemove);

            if (!removeResult.Success)
            {
                return (false, false, removeResult.Errors);
            }
        }

        if (rolesToAdd.Length > 0)
        {
            var addResult = await _users.AddToRolesAsync(user, rolesToAdd);

            if (!addResult.Success)
            {
                return (false, false, addResult.Errors);
            }
        }

        return (false, true, Array.Empty<IdentityError>());
    }

    public async Task<bool> ToggleBlockAsync(Guid id)
    {
        var user = await _users.FindByIdAsync(id);

        if (user is null)
        {
            return false;
        }

        user.Status = user.Status == UserStatus.Blocked
            ? UserStatus.Active
            : UserStatus.Blocked;

        user.UpdatedAt = DateTimeOffset.UtcNow;
        await _users.UpdateAsync(user);

        return true;
    }
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
    private readonly IAdminUserRepository _users;

    public AdminMfaService(
        IAdminMfaRepository mfa,
        IAdminUserRepository users)
    {
        _mfa = mfa;
        _users = users;
    }

    public async Task<AdminMfaIndexData> GetIndexAsync()
    {
        var users = await _users.ListForMfaAsync();

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
                RecoveryCodesLeft = await _users.CountRecoveryCodesAsync(user),
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
        var user = await _users.FindByIdAsync(userId);

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
            TwoFactorEnabled = await _users.GetTwoFactorEnabledAsync(user),
            RecoveryCodesLeft = await _users.CountRecoveryCodesAsync(user),
            HasAuthenticator = !string.IsNullOrWhiteSpace(await _users.GetAuthenticatorKeyAsync(user)),
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
