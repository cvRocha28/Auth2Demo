using Auth2Demo.Domain.Identity;

namespace Auth2Demo.Web.Areas.Admin.Models;

public sealed class MfaUserRowViewModel
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

public sealed class MfaAdminIndexViewModel
{
    public IReadOnlyList<MfaUserRowViewModel> Users { get; set; } = Array.Empty<MfaUserRowViewModel>();
    public int TotalUsers { get; set; }
    public int EnabledUsers { get; set; }
    public int WithoutMfaUsers { get; set; }
}

public sealed class MfaAdminDetailsViewModel
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public UserStatus Status { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public int RecoveryCodesLeft { get; set; }
    public bool HasAuthenticator { get; set; }
    public IReadOnlyList<Auth2Demo.Domain.Security.MfaMethod> Methods { get; set; } = Array.Empty<Auth2Demo.Domain.Security.MfaMethod>();
}


public sealed class MfaAdminAuthenticatorSetupViewModel
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsTwoFactorEnabled { get; set; }
    public string SharedKey { get; set; } = string.Empty;
    public string AuthenticatorUri { get; set; } = string.Empty;
    public string QrCodeDataUrl { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
}
