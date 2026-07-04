using Auth2Demo.Domain.Common;

namespace Auth2Demo.Domain.Security;

public sealed class SecuritySettings : AuditableEntity<Guid>
{
    public int PasswordRequiredLength { get; set; } = 8;
    public bool RequireDigit { get; set; } = true;
    public bool RequireUppercase { get; set; } = true;
    public bool RequireLowercase { get; set; } = true;
    public bool RequireNonAlphanumeric { get; set; } = true;
    public int MaxFailedAccessAttempts { get; set; } = 5;
    public int LockoutMinutes { get; set; } = 15;
    public bool RequireMfaForAdmins { get; set; }
    public int AccessTokenLifetimeMinutes { get; set; } = 60;
    public int RefreshTokenLifetimeDays { get; set; } = 14;
}
