using Auth2Demo.Web.Models.Account;

namespace Auth2Demo.Web.Models.Home;

public sealed class HomeIndexViewModel
{
    public LoginViewModel Login { get; set; } = new();
    public UserProfileViewModel? Profile { get; set; }
    public IReadOnlyList<ExternalProviderViewModel> ExternalProviders { get; set; } = Array.Empty<ExternalProviderViewModel>();
}

public sealed class UserProfileViewModel
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? UserName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Department { get; set; }
    public string? JobTitle { get; set; }
    public string? Locale { get; set; }
    public string? TimeZone { get; set; }
    public string Status { get; set; } = string.Empty;
    public string DocumentVerificationStatus { get; set; } = string.Empty;
    public string FaceVerificationStatus { get; set; } = string.Empty;
    public bool EmailConfirmed { get; set; }
    public bool PhoneNumberConfirmed { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public bool LockoutEnabled { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }
    public DateTimeOffset? LastPasswordChangeAt { get; set; }
    public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();
    public bool IsAdmin { get; set; }
    public int ActiveSessions { get; set; }
    public int TrustedDevices { get; set; }
    public int EnabledMfaMethods { get; set; }
    public int Passkeys { get; set; }
    public IReadOnlyList<UserSessionSummaryViewModel> RecentSessions { get; set; } = Array.Empty<UserSessionSummaryViewModel>();
    public IReadOnlyList<UserDeviceSummaryViewModel> RecentDevices { get; set; } = Array.Empty<UserDeviceSummaryViewModel>();
}

public sealed class UserSessionSummaryViewModel
{
    public string DeviceName { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? Location { get; set; }
    public DateTimeOffset LastSeenAt { get; set; }
    public bool IsRevoked { get; set; }
}

public sealed class UserDeviceSummaryViewModel
{
    public string Name { get; set; } = string.Empty;
    public string? Browser { get; set; }
    public string? OperatingSystem { get; set; }
    public string? IpAddress { get; set; }
    public DateTimeOffset LastSeenAt { get; set; }
    public bool IsTrusted { get; set; }
    public bool IsActive { get; set; }
}
