namespace Auth2Demo.Application.Services.Identity;

public sealed record RegisterLocalAccountRequest(
    string DisplayName,
    string Email,
    string Password,
    Func<Guid, string, string> BuildEmailConfirmationUrl);

public sealed record LoginLocalAccountRequest(
    string Login,
    string Password,
    bool RememberMe,
    Func<Guid, string, string> BuildEmailConfirmationUrl);

public sealed record ForgotPasswordRequest(
    string Login,
    Func<Guid, string, string> BuildPasswordResetUrl);

public sealed record ResetPasswordRequest(
    Guid UserId,
    string Code,
    string Password);

public sealed record ConfirmEmailRequest(Guid UserId, string Code);

public sealed record IdentityOperationError(string Code, string Description);

public enum LocalLoginStatus
{
    Succeeded,
    RequiresTwoFactor,
    EmailNotConfirmed,
    LockedOut,
    InvalidCredentials,
    BlockedOrSuspended
}

public sealed class LocalLoginResult
{
    public LocalLoginStatus Status { get; init; }
    public Guid? UserId { get; init; }
    public string? Email { get; init; }
    public string? ConfirmationLink { get; init; }
    public bool EmailDeliveryUnavailable { get; init; }
    public bool Succeeded => Status == LocalLoginStatus.Succeeded;
}

public sealed class RegisterLocalAccountResult
{
    public bool Succeeded { get; init; }
    public Guid? UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string ConfirmationLink { get; init; } = string.Empty;
    public bool EmailDeliveryUnavailable { get; init; }
    public IReadOnlyList<IdentityOperationError> Errors { get; init; } = Array.Empty<IdentityOperationError>();
}

public sealed class EmailLinkResult
{
    public bool Succeeded { get; init; }
    public bool UserFound { get; init; }
    public string Email { get; init; } = string.Empty;
    public string Link { get; init; } = string.Empty;
    public bool EmailDeliveryUnavailable { get; init; }
}

public sealed class ConfirmEmailResult
{
    public bool Succeeded { get; init; }
    public bool InvalidLink { get; init; }
    public IReadOnlyList<IdentityOperationError> Errors { get; init; } = Array.Empty<IdentityOperationError>();
}

public sealed class ResetPasswordResult
{
    public bool Succeeded { get; init; }
    public bool InvalidLink { get; init; }
    public Guid? UserId { get; init; }
    public string? Email { get; init; }
    public IReadOnlyList<IdentityOperationError> Errors { get; init; } = Array.Empty<IdentityOperationError>();
}

public interface ILocalAccountService
{
    Task<RegisterLocalAccountResult> RegisterAsync(RegisterLocalAccountRequest request);
    Task<LocalLoginResult> PasswordSignInAsync(LoginLocalAccountRequest request);
    Task<EmailLinkResult> CreateEmailConfirmationLinkAsync(string email, Func<Guid, string, string> buildUrl);
    Task<ConfirmEmailResult> ConfirmEmailAsync(ConfirmEmailRequest request);
    Task<EmailLinkResult> CreatePasswordResetLinkAsync(ForgotPasswordRequest request);
    Task<ResetPasswordResult> ResetPasswordAsync(ResetPasswordRequest request);
}
