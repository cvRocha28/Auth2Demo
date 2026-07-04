using Auth2Demo.Domain.Identity;
using Auth2Demo.Domain.Security;

namespace Auth2Demo.Application.Services.Admin;

public sealed class AdminUserCreateData
{
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool EmailConfirmed { get; set; } = true;
    public string[] Roles { get; set; } = [];
}

public sealed class AdminUserEditData
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public UserStatus Status { get; set; }
    public bool EmailConfirmed { get; set; }
    public string[] Roles { get; set; } = [];
}

public sealed class IdentityProviderListItemData
{
    public Guid Id { get; init; }
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
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Scheme { get; set; } = string.Empty;
    public IdentityProviderKind Kind { get; set; } = IdentityProviderKind.Google;
    public string? IconCssClass { get; set; }
    public string? ButtonText { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
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
