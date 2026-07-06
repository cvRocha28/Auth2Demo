using Auth2Demo.Domain.Identity;
using Auth2Demo.Domain.Security;

namespace Auth2Demo.Application.Services.Admin;

public interface IAdminAuditLogService
{
    Task<IReadOnlyList<AuditLog>> SearchAsync(string? q, string? category);
    Task<IReadOnlyList<string>> GetCategoriesAsync();
}

public interface IAdminBrandingService
{
    Task<BrandingSettings> GetAsync();
    Task SaveAsync(BrandingSettings model);
}

public interface IAdminDashboardService
{
    Task<AdminDashboardData> GetAsync();
}

public interface IAdminDeviceService
{
    Task<IReadOnlyList<UserDevice>> ListAsync();
    Task ToggleTrustedAsync(Guid id);
}

public interface IAdminEmailTemplateService
{
    Task<IReadOnlyList<EmailTemplate>> ListAsync();
    Task<EmailTemplate> GetForEditAsync(Guid id);
    Task SaveAsync(EmailTemplate model);
}

public interface IAdminHealthService
{
    Task<AdminHealthData> GetAsync();
}

public interface IAdminSecuritySettingsService
{
    Task<SecuritySettings> GetAsync();
    Task SaveAsync(SecuritySettings model);
}

public interface IAdminSessionService
{
    Task<IReadOnlyList<UserSession>> ListAsync();
    Task RevokeAsync(Guid id);
}

public interface IAdminPasskeyService
{
    Task<IReadOnlyList<PasskeyCredential>> ListAsync();
}

public interface IAdminPermissionService
{
    Task<AdminPermissionIndexData> GetIndexAsync();
    Task CreateAsync(string name, string displayName, string category, string? description);
    Task ToggleAsync(Guid roleId, Guid permissionId);
}

public interface IAdminIdentityProviderService
{
    Task<IReadOnlyList<IdentityProviderListItemData>> ListAsync();
    Task<IdentityProviderEditData?> GetForEditAsync(Guid id);
    Task<SaveIdentityProviderResult> SaveAsync(IdentityProviderEditData model);
    Task<(bool NotFound, string Message)> ToggleAsync(Guid id);
    Task<DeleteIdentityProviderResult> DeleteAsync(Guid id);
}

public interface IClientSecretGenerator
{
    string GenerateSecret(int bytes = 48);
}
