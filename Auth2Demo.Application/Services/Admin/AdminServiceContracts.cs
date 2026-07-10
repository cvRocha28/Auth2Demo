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

public interface IAdminCompanyService
{
    Task<IReadOnlyList<CompanyListItemData>> ListAsync();
    Task<IReadOnlyList<CompanyListItemData>> ListEnabledAsync();
    Task<CompanyEditData?> GetForEditAsync(Guid id);
    Task<SaveCompanyResult> SaveAsync(CompanyEditData model);
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

public interface IEnterpriseApplicationService
{
    Task<IReadOnlyList<EnterpriseApplicationListItemData>> ListAsync();
    Task<EnterpriseApplicationEditData?> GetForEditAsync(Guid applicationId);
    Task SaveAsync(SaveEnterpriseApplicationData model);
}

public interface ITenantGovernanceService
{
    Task<DirectoryOverviewData> GetDirectoryOverviewAsync();
    Task<IReadOnlyList<DirectoryUserRowData>> SearchDirectoryUsersAsync(Guid? companyId, string? query);
    Task<IReadOnlyList<DirectoryGroupRowData>> SearchDirectoryGroupsAsync(Guid? companyId, string? query);
    Task<DirectoryGroupDetailsData?> GetGroupDetailsAsync(Guid groupId);
    Task UpdateGroupAsync(Guid groupId, string name, string? description, bool isEnabled);
    Task DeleteGroupAsync(Guid groupId);
    Task<CompanyDirectoryData?> GetCompanyDirectoryAsync(Guid companyId);
    Task AddUserToCompanyAsync(Guid companyId, Guid userId, string? department, string? jobTitle, bool isDefault);
    Task UpdateCompanyUserAsync(Guid companyId, Guid membershipId, bool isEnabled, bool isDefault, string? department, string? jobTitle);
    Task RemoveUserFromCompanyAsync(Guid companyId, Guid membershipId);
    Task<Guid> CreateGroupAsync(Guid companyId, string name, string? description);
    Task AddGroupMemberAsync(Guid groupId, Guid userId);
    Task RemoveGroupMemberAsync(Guid groupId, Guid userId);
    Task<EnterpriseAssignmentPageData?> GetAssignmentsAsync(Guid applicationId);
    Task AssignAsync(Guid applicationId, Guid companyId, EnterpriseAssignmentType type, Guid principalId, Guid? roleId);
    Task RemoveAssignmentAsync(Guid assignmentId);
    Task<Guid> SaveRoleAsync(Guid applicationId, Guid? roleId, string name, string value, string? description, bool enabled);
}
public interface IEnterpriseApplicationAccessEvaluator
{
    Task<EnterpriseAccessResult> EvaluateAsync(Guid userId, Guid applicationId, CancellationToken cancellationToken = default);
}
