using Auth2Demo.Domain.Security;
using Auth2Demo.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace Auth2Demo.Web.Models.Perfil;

public sealed record PasswordPolicyViewModel(
    int RequiredLength,
    bool RequireDigit,
    bool RequireUppercase,
    bool RequireLowercase,
    bool RequireNonAlphanumeric);

public sealed class PerfilIndexViewModel
{
    public required ApplicationUser User { get; init; }
    public required string DisplayName { get; set; }
    public required string CurrentLocale { get; init; }
    public required bool IsAdmin { get; init; }
    public required IReadOnlyList<UserLoginInfo> ExternalLogins { get; init; }
    public required IReadOnlyList<UserSession> Sessions { get; init; }
    public required IReadOnlyList<UserDevice> Devices { get; init; }
    public required IReadOnlyList<AuditLog> AuditLogs { get; init; }
    public required IReadOnlyList<MfaMethod> MfaMethods { get; init; }
    public required IReadOnlyList<PasskeyCredential> Passkeys { get; init; }
    public required bool HasLocalPassword { get; init; }
    public required PasswordPolicyViewModel PasswordPolicy { get; init; }

    public bool HasMfa => User.TwoFactorEnabled || MfaMethods.Count > 0;
    public bool HasExternalLogin => ExternalLogins.Count > 0;
    public bool HasTrustedDevice => Devices.Any(x => x.IsTrusted);
}
