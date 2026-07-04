using Auth2Demo.Application.Services.Admin;
using Auth2Demo.Domain.Security;
using Auth2Demo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Auth2Demo.Domain.Identity;

namespace Auth2Demo.Infrastructure.Repositories.Admin;

public sealed class AdminAuditLogRepository : IAdminAuditLogRepository
{
    private readonly ApplicationDbContext _db;

    public AdminAuditLogRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<AuditLog>> SearchAsync(string? query, string? category)
    {
        var auditLogs = _db.AuditLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query))
        {
            auditLogs = auditLogs.Where(x =>
                (x.UserEmail ?? string.Empty).Contains(query) ||
                x.EventType.Contains(query) ||
                (x.Description ?? string.Empty).Contains(query));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            auditLogs = auditLogs.Where(x => x.Category == category);
        }

        return await auditLogs
            .OrderByDescending(x => x.CreatedAt)
            .Take(200)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<string>> GetCategoriesAsync()
    {
        return await _db.AuditLogs
            .AsNoTracking()
            .Select(x => x.Category)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync();
    }

    public Task<int> CountLoginEventsFromAsync(DateTimeOffset from)
    {
        return _db.AuditLogs.CountAsync(x =>
            x.CreatedAt >= from &&
            x.EventType.Contains("Login"));
    }

    public Task<int> CountFailedLoginEventsFromAsync(DateTimeOffset from)
    {
        return _db.AuditLogs.CountAsync(x =>
            x.CreatedAt >= from &&
            x.EventType.Contains("Login") &&
            x.Outcome != "Success");
    }

    public async Task<IReadOnlyList<AuditLog>> GetRecentAsync(int take)
    {
        return await _db.AuditLogs
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .Take(take)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<AdminDashboardProviderUsageData>> GetProviderUsageAsync(int take)
    {
        return await _db.AuditLogs
            .AsNoTracking()
            .Where(x => x.EventType.Contains("Login") && x.Provider != null && x.Provider != string.Empty)
            .GroupBy(x => x.Provider!)
            .Select(g => new AdminDashboardProviderUsageData
            {
                Provider = g.Key,
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .Take(take)
            .ToListAsync();
    }

    public Task<DateTimeOffset?> GetLastAuditAtAsync()
    {
        return _db.AuditLogs
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => (DateTimeOffset?)x.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task AddAsync(AuditLog auditLog)
    {
        _db.AuditLogs.Add(auditLog);
        await _db.SaveChangesAsync();
    }
}

public sealed class AdminBrandingRepository : IAdminBrandingRepository
{
    private readonly ApplicationDbContext _db;

    public AdminBrandingRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<BrandingSettings?> GetAsync()
    {
        return _db.BrandingSettings.FirstOrDefaultAsync();
    }

    public async Task SaveAsync(BrandingSettings model)
    {
        var current = await _db.BrandingSettings.FirstOrDefaultAsync();

        if (current is null)
        {
            _db.BrandingSettings.Add(model);
        }
        else
        {
            current.TenantName = model.TenantName;
            current.LogoUrl = model.LogoUrl;
            current.FaviconUrl = model.FaviconUrl;
            current.PrimaryColor = model.PrimaryColor;
            current.SecondaryColor = model.SecondaryColor;
            current.Theme = model.Theme;
            current.CustomCss = model.CustomCss;
        }

        await _db.SaveChangesAsync();
    }
}

public sealed class AdminDashboardRepository : IAdminDashboardRepository
{
    private readonly ApplicationDbContext _db;

    public AdminDashboardRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<int> CountEnabledIdentityProvidersAsync()
    {
        return _db.IdentityProviders.CountAsync(x => x.IsEnabled);
    }

    public Task<int> CountActiveSessionsAsync()
    {
        return _db.UserSessions.CountAsync(x => !x.IsRevoked);
    }

    public Task<int> CountTrustedDevicesAsync()
    {
        return _db.UserDevices.CountAsync(x => x.IsTrusted);
    }
}

public sealed class AdminDeviceRepository : IAdminDeviceRepository
{
    private readonly ApplicationDbContext _db;

    public AdminDeviceRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<UserDevice>> ListAsync(int take)
    {
        return await _db.UserDevices
            .AsNoTracking()
            .OrderByDescending(x => x.LastSeenAt)
            .Take(take)
            .ToListAsync();
    }

    public async Task ToggleTrustedAsync(Guid id)
    {
        var device = await _db.UserDevices.FindAsync(id);

        if (device is null)
        {
            return;
        }

        device.IsTrusted = !device.IsTrusted;
        await _db.SaveChangesAsync();
    }
}

public sealed class AdminEmailTemplateRepository : IAdminEmailTemplateRepository
{
    private readonly ApplicationDbContext _db;

    public AdminEmailTemplateRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<EmailTemplate>> ListAsync()
    {
        return await _db.EmailTemplates
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync();
    }

    public Task<EmailTemplate?> GetAsync(Guid id)
    {
        return _db.EmailTemplates.FindAsync(id).AsTask();
    }

    public async Task SaveAsync(EmailTemplate model)
    {
        var current = await _db.EmailTemplates.FindAsync(model.Id);

        if (current is null)
        {
            _db.EmailTemplates.Add(model);
        }
        else
        {
            current.Key = model.Key;
            current.Name = model.Name;
            current.Subject = model.Subject;
            current.BodyHtml = model.BodyHtml;
            current.IsEnabled = model.IsEnabled;
        }

        await _db.SaveChangesAsync();
    }
}

public sealed class AdminHealthRepository : IAdminHealthRepository
{
    private readonly ApplicationDbContext _db;

    public AdminHealthRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<bool> CanConnectDatabaseAsync()
    {
        return _db.Database.CanConnectAsync();
    }

    public Task<int> CountEnabledIdentityProvidersAsync()
    {
        return _db.IdentityProviders.CountAsync(x => x.IsEnabled);
    }
}

public sealed class AdminSecuritySettingsRepository : IAdminSecuritySettingsRepository
{
    private readonly ApplicationDbContext _db;

    public AdminSecuritySettingsRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<SecuritySettings?> GetAsync()
    {
        return _db.SecuritySettings.FirstOrDefaultAsync();
    }

    public async Task SaveAsync(SecuritySettings model)
    {
        var current = await _db.SecuritySettings.FirstOrDefaultAsync();

        if (current is null)
        {
            _db.SecuritySettings.Add(model);
        }
        else
        {
            current.PasswordRequiredLength = model.PasswordRequiredLength;
            current.RequireDigit = model.RequireDigit;
            current.RequireUppercase = model.RequireUppercase;
            current.RequireLowercase = model.RequireLowercase;
            current.RequireNonAlphanumeric = model.RequireNonAlphanumeric;
            current.MaxFailedAccessAttempts = model.MaxFailedAccessAttempts;
            current.LockoutMinutes = model.LockoutMinutes;
            current.RequireMfaForAdmins = model.RequireMfaForAdmins;
            current.AccessTokenLifetimeMinutes = model.AccessTokenLifetimeMinutes;
            current.RefreshTokenLifetimeDays = model.RefreshTokenLifetimeDays;
        }

        await _db.SaveChangesAsync();
    }
}

public sealed class AdminSessionRepository : IAdminSessionRepository
{
    private readonly ApplicationDbContext _db;

    public AdminSessionRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<UserSession>> ListAsync(int take)
    {
        return await _db.UserSessions
            .AsNoTracking()
            .OrderByDescending(x => x.LastSeenAt)
            .Take(take)
            .ToListAsync();
    }

    public async Task RevokeAsync(Guid id)
    {
        var session = await _db.UserSessions.FindAsync(id);

        if (session is null)
        {
            return;
        }

        session.IsRevoked = true;
        session.RevokedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();
    }
}

public sealed class AdminPasskeyRepository : IAdminPasskeyRepository
{
    private readonly ApplicationDbContext _db;

    public AdminPasskeyRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<PasskeyCredential>> ListAsync()
    {
        return await _db.PasskeyCredentials
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
    }
}

public sealed class AdminPermissionRepository : IAdminPermissionRepository
{
    private readonly ApplicationDbContext _db;

    public AdminPermissionRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<AdminRolePermissionData>> GetRolePermissionsAsync()
    {
        return await _db.RolePermissions
            .AsNoTracking()
            .Select(x => new AdminRolePermissionData(x.RoleId, x.PermissionId))
            .ToListAsync();
    }

    public async Task<IReadOnlyList<AdminPermissionData>> GetPermissionsAsync()
    {
        return await _db.Permissions
            .AsNoTracking()
            .OrderBy(x => x.Category)
            .ThenBy(x => x.Name)
            .Select(x => new AdminPermissionData(x.Id, x.Name, x.DisplayName, x.Category))
            .ToListAsync();
    }

    public Task<bool> PermissionExistsAsync(string name)
    {
        return _db.Permissions.AnyAsync(x => x.Name == name);
    }

    public async Task CreateAsync(Permission permission)
    {
        _db.Permissions.Add(permission);
        await _db.SaveChangesAsync();
    }

    public async Task ToggleAsync(Guid roleId, Guid permissionId)
    {
        var link = await _db.RolePermissions.FirstOrDefaultAsync(x =>
            x.RoleId == roleId &&
            x.PermissionId == permissionId);

        if (link is null)
        {
            _db.RolePermissions.Add(new RolePermission
            {
                Id = Guid.NewGuid(),
                RoleId = roleId,
                PermissionId = permissionId
            });
        }
        else
        {
            _db.RolePermissions.Remove(link);
        }

        await _db.SaveChangesAsync();
    }
}

public sealed class AdminIdentityProviderRepository : IAdminIdentityProviderRepository
{
    private readonly ApplicationDbContext _db;

    public AdminIdentityProviderRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<IdentityProviderListItemData>> ListAsync()
    {
        return await _db.IdentityProviders
            .AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.DisplayName)
            .Select(x => new IdentityProviderListItemData
            {
                Id = x.Id,
                Name = x.Name,
                DisplayName = x.DisplayName,
                Scheme = x.Scheme,
                Kind = x.Kind,
                IsEnabled = x.IsEnabled,
                IsSystemProvider = x.IsSystemProvider,
                SortOrder = x.SortOrder,
                HasClientId = x.ClientId != null && x.ClientId != string.Empty,
                HasClientSecret = x.ClientSecret != null && x.ClientSecret != string.Empty
            })
            .ToListAsync();
    }

    public Task<IdentityProvider?> GetAsync(Guid id, bool tracking)
    {
        var query = tracking ? _db.IdentityProviders : _db.IdentityProviders.AsNoTracking();
        return query.FirstOrDefaultAsync(x => x.Id == id);
    }

    public Task<bool> HasDuplicateAsync(Guid currentId, string name, string scheme)
    {
        return _db.IdentityProviders.AnyAsync(x =>
            x.Id != currentId &&
            (x.Name == name || x.Scheme == scheme));
    }

    public async Task AddAsync(IdentityProvider provider)
    {
        _db.IdentityProviders.Add(provider);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(IdentityProvider provider)
    {
        _db.IdentityProviders.Remove(provider);
        await _db.SaveChangesAsync();
    }

    public Task SaveChangesAsync()
    {
        return _db.SaveChangesAsync();
    }
}

public sealed class AdminMfaRepository : IAdminMfaRepository
{
    private readonly ApplicationDbContext _db;

    public AdminMfaRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<MfaMethod>> ListMethodsAsync()
    {
        return await _db.MfaMethods
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IReadOnlyList<MfaMethod>> ListMethodsByUserAsync(Guid userId)
    {
        return await _db.MfaMethods
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.Method)
            .ToListAsync();
    }

    public Task<MfaMethod?> GetMethodByNameAsync(Guid userId, string method)
    {
        return _db.MfaMethods.FirstOrDefaultAsync(x =>
            x.UserId == userId &&
            x.Method == method);
    }

    public Task<MfaMethod?> GetMethodByIdAsync(Guid userId, Guid methodId)
    {
        return _db.MfaMethods.FirstOrDefaultAsync(x =>
            x.Id == methodId &&
            x.UserId == userId);
    }

    public async Task UpsertMethodAsync(MfaMethod method)
    {
        var current = await _db.MfaMethods.FirstOrDefaultAsync(x =>
            x.UserId == method.UserId &&
            x.Method == method.Method);

        if (current is null)
        {
            _db.MfaMethods.Add(method);
        }
        else
        {
            current.UserEmail = method.UserEmail;
            current.DisplayName = method.DisplayName;
            current.IsEnabled = method.IsEnabled;
            current.IsDefault = method.IsDefault;
            current.LastUsedAt = method.LastUsedAt ?? current.LastUsedAt;
        }

        await _db.SaveChangesAsync();
    }

    public async Task DeleteMethodAsync(MfaMethod method)
    {
        _db.MfaMethods.Remove(method);
        await _db.SaveChangesAsync();
    }

    public async Task AddAuditAsync(AuditLog auditLog)
    {
        _db.AuditLogs.Add(auditLog);
        await _db.SaveChangesAsync();
    }
}
