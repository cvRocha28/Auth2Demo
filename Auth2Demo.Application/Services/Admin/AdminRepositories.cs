using Auth2Demo.Domain.Identity;
using Auth2Demo.Domain.Security;

namespace Auth2Demo.Application.Services.Admin;

public interface IAdminAuditLogRepository
{
    Task<IReadOnlyList<AuditLog>> SearchAsync(string? query, string? category);
    Task<IReadOnlyList<string>> GetCategoriesAsync();
    Task<int> CountLoginEventsFromAsync(DateTimeOffset from);
    Task<int> CountFailedLoginEventsFromAsync(DateTimeOffset from);
    Task<IReadOnlyList<AuditLog>> GetRecentAsync(int take);
    Task<IReadOnlyList<AdminDashboardProviderUsageData>> GetProviderUsageAsync(int take);
    Task<DateTimeOffset?> GetLastAuditAtAsync();
    Task AddAsync(AuditLog auditLog);
}

public interface IAdminBrandingRepository
{
    Task<BrandingSettings?> GetAsync();
    Task SaveAsync(BrandingSettings model);
}

public interface IAdminDashboardRepository
{
    Task<int> CountEnabledIdentityProvidersAsync();
    Task<int> CountActiveSessionsAsync();
    Task<int> CountTrustedDevicesAsync();
}

public interface IAdminDeviceRepository
{
    Task<IReadOnlyList<UserDevice>> ListAsync(int take);
    Task ToggleTrustedAsync(Guid id);
}

public interface IAdminEmailTemplateRepository
{
    Task<IReadOnlyList<EmailTemplate>> ListAsync();
    Task<EmailTemplate?> GetAsync(Guid id);
    Task SaveAsync(EmailTemplate model);
}

public interface IAdminHealthRepository
{
    Task<bool> CanConnectDatabaseAsync();
    Task<int> CountEnabledIdentityProvidersAsync();
}

public interface IAdminSecuritySettingsRepository
{
    Task<SecuritySettings?> GetAsync();
    Task SaveAsync(SecuritySettings model);
}

public interface IAdminSessionRepository
{
    Task<IReadOnlyList<UserSession>> ListAsync(int take);
    Task RevokeAsync(Guid id);
}

public interface IAdminPasskeyRepository
{
    Task<IReadOnlyList<PasskeyCredential>> ListAsync();
}

public interface IAdminPermissionRepository
{
    Task<IReadOnlyList<AdminRolePermissionData>> GetRolePermissionsAsync();
    Task<IReadOnlyList<AdminPermissionData>> GetPermissionsAsync();
    Task<bool> PermissionExistsAsync(string name);
    Task CreateAsync(Permission permission);
    Task ToggleAsync(Guid roleId, Guid permissionId);
}

public interface IAdminCompanyRepository
{
    Task<IReadOnlyList<CompanyListItemData>> ListAsync();
    Task<IReadOnlyList<CompanyListItemData>> ListEnabledAsync();
    Task<Company?> GetAsync(Guid id, bool tracking);
    Task<bool> HasDuplicateAsync(Guid currentId, string name);
    Task AddAsync(Company company);
    Task SaveChangesAsync();
}

public interface IAdminIdentityProviderRepository
{
    Task<IReadOnlyList<IdentityProviderListItemData>> ListAsync();
    Task<IdentityProvider?> GetAsync(Guid id, bool tracking);
    Task<bool> HasDuplicateAsync(Guid currentId, string name, string scheme);
    Task AddAsync(IdentityProvider provider);
    Task DeleteAsync(IdentityProvider provider);
    Task SaveChangesAsync();
}

public interface IAdminMfaRepository
{
    Task<IReadOnlyList<MfaMethod>> ListMethodsAsync();
    Task<IReadOnlyList<MfaMethod>> ListMethodsByUserAsync(Guid userId);
    Task<MfaMethod?> GetMethodByNameAsync(Guid userId, string method);
    Task<MfaMethod?> GetMethodByIdAsync(Guid userId, Guid methodId);
    Task UpsertMethodAsync(MfaMethod method);
    Task DeleteMethodAsync(MfaMethod method);
    Task AddAuditAsync(AuditLog auditLog);
}


public interface IEnterpriseApplicationRepository
{
    Task<IReadOnlyList<EnterpriseApplicationListItemData>> ListAsync();
    Task<EnterpriseApplicationEditData?> GetForEditAsync(Guid applicationId);
    Task SaveAsync(SaveEnterpriseApplicationData model);
}
