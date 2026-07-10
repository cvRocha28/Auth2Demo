using Auth2Demo.Domain.Identity;
using Auth2Demo.Domain.Security;

namespace Auth2Demo.Application.Services.Admin;

public sealed class AdminUserCreateData
{
    public string DisplayName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool EmailConfirmed { get; set; } = true;
    public string[] Roles { get; set; } = [];
}

public sealed class AdminUserEditData
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public UserStatus Status { get; set; }
    public bool EmailConfirmed { get; set; }
    public string[] Roles { get; set; } = [];
}

public sealed class CompanyListItemData
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string? DomainHint { get; init; }
    public string? Country { get; init; }
    public string? Culture { get; init; }
    public bool IsEnabled { get; init; }
    public bool IsDefault { get; init; }
    public int ProviderCount { get; init; }
}

public sealed class CompanyEditData
{
    public Guid? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? DomainHint { get; set; }
    public string? Country { get; set; }
    public string? Culture { get; set; }
    public string? TimeZone { get; set; }
    public bool IsEnabled { get; set; } = true;
    public bool IsDefault { get; set; }
}

public sealed record SaveCompanyResult(bool NotFound, bool Duplicated, bool Created);

public sealed class IdentityProviderListItemData
{
    public Guid Id { get; init; }
    public Guid? CompanyId { get; init; }
    public string? CompanyName { get; init; }
    public string Name { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Scheme { get; init; } = string.Empty;
    public IdentityProviderKind Kind { get; init; }
    public bool IsEnabled { get; init; }
    public bool IsSystemProvider { get; init; }
    public int SortOrder { get; init; }
    public bool HasClientId { get; init; }
    public bool HasClientSecret { get; init; }
}

public sealed class IdentityProviderEditData
{
    public Guid? Id { get; set; }
    public Guid? CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Scheme { get; set; } = string.Empty;
    public IdentityProviderKind Kind { get; set; } = IdentityProviderKind.Google;
    public string? IconCssClass { get; set; }
    public string? ButtonText { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public bool HasClientSecret { get; set; }
    public string? Authority { get; set; }
    public string? CallbackPath { get; set; }
    public bool IsEnabled { get; set; }
    public bool IsSystemProvider { get; set; }
    public int SortOrder { get; set; } = 100;
}

public sealed class AdminDashboardProviderUsageData
{
    public string Provider { get; init; } = string.Empty;
    public int Count { get; init; }
}

public sealed record AdminDashboardData(
    long Users,
    long NewUsersToday,
    long Applications,
    long Scopes,
    int IdentityProviders,
    int LoginsToday,
    int FailedLoginsToday,
    int ActiveSessions,
    int TrustedDevices,
    IReadOnlyList<AuditLog> RecentAudits,
    IReadOnlyList<AdminDashboardProviderUsageData> ProviderUsage);

public sealed record AdminHealthData(bool DatabaseOk, DateTimeOffset? LastAudit, int ProviderCount);

public sealed record SaveIdentityProviderResult(bool NotFound, bool Duplicated, bool Created);

public sealed record DeleteIdentityProviderResult(bool NotFound, bool IsSystemProvider);

public sealed class AdminMfaUserRowData
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public UserStatus Status { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public int RecoveryCodesLeft { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }
    public DateTimeOffset? LastMfaUsedAt { get; set; }
    public IReadOnlyList<string> Methods { get; set; } = Array.Empty<string>();
}

public sealed class AdminMfaIndexData
{
    public IReadOnlyList<AdminMfaUserRowData> Users { get; set; } = Array.Empty<AdminMfaUserRowData>();
    public int TotalUsers { get; set; }
    public int EnabledUsers { get; set; }
    public int WithoutMfaUsers { get; set; }
}

public sealed class AdminMfaDetailsData
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public UserStatus Status { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public int RecoveryCodesLeft { get; set; }
    public bool HasAuthenticator { get; set; }
    public IReadOnlyList<MfaMethod> Methods { get; set; } = Array.Empty<MfaMethod>();
}

public sealed record AdminPermissionRoleData(Guid Id, string? Name);

public sealed record AdminPermissionData(Guid Id, string Name, string DisplayName, string Category);

public sealed record AdminRolePermissionData(Guid RoleId, Guid PermissionId);

public sealed record AdminPermissionIndexData(
    IReadOnlyList<AdminPermissionRoleData> Roles,
    IReadOnlyList<AdminRolePermissionData> RolePermissions,
    IReadOnlyList<AdminPermissionData> Permissions);

public sealed class EnterpriseApplicationListItemData
{
    public Guid ApplicationId { get; init; }
    public string ClientId { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string? OwnerCompanyName { get; init; }
    public int AllowedCompanyCount { get; init; }
    public int AllowedProviderCount { get; init; }
    public int UserAssignmentRequiredCount { get; init; }
    public bool IsEnabled { get; init; }
}

public sealed class EnterpriseApplicationEditData
{
    public Guid ApplicationId { get; init; }
    public string ClientId { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public bool IsEnabled { get; init; }
    public Guid? OwnerCompanyId { get; set; }
    public IReadOnlyList<EnterpriseTenantAccessData> TenantAccess { get; init; } = Array.Empty<EnterpriseTenantAccessData>();
    public IReadOnlyList<IdentityProviderListItemData> Providers { get; init; } = Array.Empty<IdentityProviderListItemData>();
    public HashSet<Guid> AllowedProviderIds { get; init; } = new();
}

public sealed class EnterpriseTenantAccessData
{
    public Guid CompanyId { get; init; }
    public string CompanyName { get; init; } = string.Empty;
    public string? DomainHint { get; init; }
    public string? Country { get; init; }
    public string? Culture { get; init; }
    public int ProviderCount { get; init; }
    public bool IsDefault { get; init; }
    public bool IsEnabled { get; init; }
    public bool RequireUserAssignment { get; init; }
    public string? Notes { get; init; }
}

public sealed class SaveEnterpriseApplicationData
{
    public Guid ApplicationId { get; set; }
    public bool IsEnabled { get; set; }
    public Guid? OwnerCompanyId { get; set; }
    public EnterpriseTenantAccessInput[] TenantAccess { get; set; } = [];
    public Guid[] AllowedProviderIds { get; set; } = [];
}

public sealed class EnterpriseTenantAccessInput
{
    public Guid CompanyId { get; set; }
    public bool IsEnabled { get; set; }
    public bool RequireUserAssignment { get; set; }
    public string? Notes { get; set; }
}

public sealed class DirectoryOverviewData
{
    public int TotalUsers { get; init; }
    public int ActiveUsers { get; init; }
    public int BlockedUsers { get; init; }
    public int ActiveMemberships { get; init; }
    public int DisabledMemberships { get; init; }
    public int TotalGroups { get; init; }
    public int ActiveGroups { get; init; }
    public int TotalCompanies { get; init; }
    public int ActiveCompanies { get; init; }
    public int UnassignedUsers { get; init; }
    public int ApplicationAssignments { get; init; }
    public IReadOnlyList<DirectoryCompanySummaryData> Companies { get; init; } = Array.Empty<DirectoryCompanySummaryData>();
}
public sealed class DirectoryCompanySummaryData
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? DomainHint { get; init; }
    public string? Country { get; init; }
    public string? Culture { get; init; }
    public int UserCount { get; init; }
    public int DisabledUserCount { get; init; }
    public int GroupCount { get; init; }
    public int DisabledGroupCount { get; init; }
    public int ProviderCount { get; init; }
    public int ApplicationAssignmentCount { get; init; }
    public bool IsActive { get; init; }
}
public sealed class DirectoryUserRowData
{
    public Guid Id { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public bool IsBlocked { get; init; }
    public int CompanyCount { get; init; }
    public int GroupCount { get; init; }
    public string Companies { get; init; } = string.Empty;
}
public sealed class DirectoryGroupRowData
{
    public Guid Id { get; init; }
    public Guid CompanyId { get; init; }
    public string CompanyName { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsEnabled { get; init; }
    public int MemberCount { get; init; }
    public int ApplicationAssignmentCount { get; init; }
}
public sealed class DirectoryGroupDetailsData
{
    public Guid Id { get; init; }
    public Guid CompanyId { get; init; }
    public string CompanyName { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsEnabled { get; init; }
    public IReadOnlyList<DirectoryGroupMemberData> Members { get; init; } = Array.Empty<DirectoryGroupMemberData>();
    public IReadOnlyList<DirectoryUserOptionData> AvailableUsers { get; init; } = Array.Empty<DirectoryUserOptionData>();
    public int ActiveUserCount { get; init; }
    public int DisabledUserCount { get; init; }
    public int ActiveGroupCount { get; init; }
    public int ApplicationAssignmentCount { get; init; }
}
public sealed class DirectoryGroupMemberData
{
    public Guid UserId { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? Department { get; init; }
    public string? JobTitle { get; init; }
}

public sealed class CompanyDirectoryData
{
    public Guid CompanyId { get; init; }
    public string CompanyName { get; init; } = string.Empty;
    public IReadOnlyList<CompanyUserData> Users { get; init; } = Array.Empty<CompanyUserData>();
    public IReadOnlyList<CompanyGroupData> Groups { get; init; } = Array.Empty<CompanyGroupData>();
    public IReadOnlyList<DirectoryUserOptionData> AvailableUsers { get; init; } = Array.Empty<DirectoryUserOptionData>();
    public int ActiveUserCount { get; init; }
    public int DisabledUserCount { get; init; }
    public int ActiveGroupCount { get; init; }
    public int ApplicationAssignmentCount { get; init; }
}
public sealed class CompanyUserData
{
    public Guid MembershipId { get; init; }
    public Guid UserId { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? Department { get; init; }
    public string? JobTitle { get; init; }
    public bool IsEnabled { get; init; }
    public bool IsDefault { get; init; }
}
public sealed class CompanyGroupData
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsEnabled { get; init; }
    public int MemberCount { get; init; }
}
public sealed class EnterpriseAssignmentPageData
{
    public Guid ApplicationId { get; init; }
    public string ApplicationName { get; init; } = string.Empty;
    public string ClientId { get; init; } = string.Empty;
    public IReadOnlyList<EnterpriseAssignmentRowData> Assignments { get; init; } = Array.Empty<EnterpriseAssignmentRowData>();
    public IReadOnlyList<EnterpriseApplicationRoleData> Roles { get; init; } = Array.Empty<EnterpriseApplicationRoleData>();
    public IReadOnlyList<EnterpriseAssignablePrincipalData> Principals { get; init; } = Array.Empty<EnterpriseAssignablePrincipalData>();
}
public sealed class EnterpriseAssignmentRowData
{
    public Guid Id { get; init; }
    public Guid CompanyId { get; init; }
    public string CompanyName { get; init; } = string.Empty;
    public string PrincipalName { get; init; } = string.Empty;
    public EnterpriseAssignmentType PrincipalType { get; init; }
    public string? RoleName { get; init; }
    public bool IsEnabled { get; init; }
}
public sealed class EnterpriseAssignablePrincipalData
{
    public Guid Id { get; init; }
    public Guid CompanyId { get; init; }
    public string CompanyName { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public EnterpriseAssignmentType Type { get; init; }
}
public sealed class EnterpriseApplicationRoleData
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsEnabled { get; init; }
}
public sealed record EnterpriseAccessResult(bool IsAllowed, string? DenialReason, Guid? CompanyId, IReadOnlyCollection<string> Roles);

public sealed class DirectoryUserOptionData
{
    public Guid Id { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
}
