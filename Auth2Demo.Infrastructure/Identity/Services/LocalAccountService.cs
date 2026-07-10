using System.Text;
using Auth2Demo.Application.Services.Identity;
using Auth2Demo.Domain.Identity;
using Auth2Demo.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.WebUtilities;

namespace Auth2Demo.Infrastructure.Identity.Services;

public sealed class LocalAccountService : ILocalAccountService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IIdentityEmailSender _emailSender;
    private readonly ApplicationDbContext _db;

    public LocalAccountService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IIdentityEmailSender emailSender,
        ApplicationDbContext db)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _emailSender = emailSender;
        _db = db;
    }

    public async Task<RegisterLocalAccountResult> RegisterAsync(RegisterLocalAccountRequest request)
    {
        await ApplyRuntimeLockoutSettingsAsync();
        var email = NormalizeLoginInput(request.Email);

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            DisplayName = request.DisplayName,
            Status = UserStatus.Active,
            EmailConfirmed = false
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return new RegisterLocalAccountResult
            {
                Email = email,
                Errors = ToErrors(result.Errors)
            };
        }

        var confirmationLink = await BuildEmailConfirmationLinkAsync(user, request.BuildEmailConfirmationUrl);
        var emailDeliveryUnavailable = await TrySendEmailConfirmationAsync(user.Email ?? email, confirmationLink);

        return new RegisterLocalAccountResult
        {
            Succeeded = true,
            UserId = user.Id,
            Email = user.Email ?? email,
            ConfirmationLink = confirmationLink,
            EmailDeliveryUnavailable = emailDeliveryUnavailable
        };
    }

    public async Task<LocalLoginResult> PasswordSignInAsync(LoginLocalAccountRequest request)
    {
        await ApplyRuntimeLockoutSettingsAsync();
        var login = NormalizeLoginInput(request.Login);
        var user = await FindByUserNameOrEmailAsync(login);
        if (user is null)
        {
            return new LocalLoginResult { Status = LocalLoginStatus.InvalidCredentials, Email = login };
        }

        if (user.Status is UserStatus.Blocked or UserStatus.Suspended)
        {
            return new LocalLoginResult { Status = LocalLoginStatus.BlockedOrSuspended, UserId = user.Id, Email = user.Email };
        }

        if (!await _userManager.IsEmailConfirmedAsync(user))
        {
            var validPassword = await _userManager.CheckPasswordAsync(user, request.Password);
            if (!validPassword)
            {
                await _userManager.AccessFailedAsync(user);
                return new LocalLoginResult { Status = LocalLoginStatus.InvalidCredentials, UserId = user.Id, Email = user.Email };
            }

            var confirmationLink = await BuildEmailConfirmationLinkAsync(user, request.BuildEmailConfirmationUrl);
            var emailDeliveryUnavailable = await TrySendEmailConfirmationAsync(user.Email ?? login, confirmationLink);

            return new LocalLoginResult
            {
                Status = LocalLoginStatus.EmailNotConfirmed,
                UserId = user.Id,
                Email = user.Email,
                ConfirmationLink = confirmationLink,
                EmailDeliveryUnavailable = emailDeliveryUnavailable
            };
        }

        var result = await _signInManager.PasswordSignInAsync(
            user,
            request.Password,
            request.RememberMe,
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            user.LastLoginAt = DateTimeOffset.UtcNow;
            await _userManager.UpdateAsync(user);
            return new LocalLoginResult { Status = LocalLoginStatus.Succeeded, UserId = user.Id, Email = user.Email };
        }

        if (result.RequiresTwoFactor)
        {
            return new LocalLoginResult { Status = LocalLoginStatus.RequiresTwoFactor, UserId = user.Id, Email = user.Email };
        }

        if (result.IsLockedOut)
        {
            return new LocalLoginResult { Status = LocalLoginStatus.LockedOut, UserId = user.Id, Email = user.Email };
        }

        return new LocalLoginResult { Status = LocalLoginStatus.InvalidCredentials, UserId = user.Id, Email = user.Email };
    }

    public async Task<EmailLinkResult> CreateEmailConfirmationLinkAsync(string email, Func<Guid, string, string> buildUrl)
    {
        var login = NormalizeLoginInput(email);
        var user = await FindByUserNameOrEmailAsync(login);
        if (user is null || string.IsNullOrWhiteSpace(user.Email))
        {
            return new EmailLinkResult { Email = login };
        }

        if (await _userManager.IsEmailConfirmedAsync(user))
        {
            return new EmailLinkResult { Succeeded = true, UserFound = true, Email = user.Email };
        }

        var link = await BuildEmailConfirmationLinkAsync(user, buildUrl);
        var emailDeliveryUnavailable = await TrySendEmailConfirmationAsync(user.Email, link);

        return new EmailLinkResult
        {
            Succeeded = true,
            UserFound = true,
            Email = user.Email,
            Link = link,
            EmailDeliveryUnavailable = emailDeliveryUnavailable
        };
    }

    public async Task<ConfirmEmailResult> ConfirmEmailAsync(ConfirmEmailRequest request)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString());
        if (user is null || string.IsNullOrWhiteSpace(request.Code))
        {
            return new ConfirmEmailResult { InvalidLink = true };
        }

        string decodedCode;
        try
        {
            decodedCode = DecodeToken(request.Code);
        }
        catch (FormatException)
        {
            return new ConfirmEmailResult { InvalidLink = true };
        }

        var result = await _userManager.ConfirmEmailAsync(user, decodedCode);
        return new ConfirmEmailResult
        {
            Succeeded = result.Succeeded,
            Errors = ToErrors(result.Errors)
        };
    }

    public async Task<EmailLinkResult> CreatePasswordResetLinkAsync(ForgotPasswordRequest request)
    {
        var login = NormalizeLoginInput(request.Login);
        var user = await FindByUserNameOrEmailAsync(login);
        if (user is null || !await _userManager.HasPasswordAsync(user) || string.IsNullOrWhiteSpace(user.Email))
        {
            return new EmailLinkResult { Succeeded = true, Email = login };
        }

        var code = await _userManager.GeneratePasswordResetTokenAsync(user);
        var encodedCode = EncodeToken(code);
        var resetLink = request.BuildPasswordResetUrl(user.Id, encodedCode);
        var emailDeliveryUnavailable = await TrySendPasswordResetAsync(user.Email, resetLink);

        return new EmailLinkResult
        {
            Succeeded = true,
            UserFound = true,
            Email = user.Email,
            Link = resetLink,
            EmailDeliveryUnavailable = emailDeliveryUnavailable
        };
    }

    public async Task<ResetPasswordResult> ResetPasswordAsync(ResetPasswordRequest request)
    {
        var user = await _userManager.FindByIdAsync(request.UserId.ToString());
        if (user is null || string.IsNullOrWhiteSpace(request.Code))
        {
            return new ResetPasswordResult { InvalidLink = true };
        }

        string decodedCode;
        try
        {
            decodedCode = DecodeToken(request.Code);
        }
        catch (FormatException)
        {
            return new ResetPasswordResult { InvalidLink = true };
        }

        var result = await _userManager.ResetPasswordAsync(user, decodedCode, request.Password);
        return new ResetPasswordResult
        {
            Succeeded = result.Succeeded,
            InvalidLink = false,
            UserId = user.Id,
            Email = user.Email,
            Errors = ToErrors(result.Errors)
        };
    }


    private async Task<ApplicationUser?> FindByUserNameOrEmailAsync(string login)
    {
        if (string.IsNullOrWhiteSpace(login))
        {
            return null;
        }

        var user = await _userManager.FindByNameAsync(login);
        if (user is not null)
        {
            return user;
        }

        return login.Contains('@', StringComparison.Ordinal)
            ? await _userManager.FindByEmailAsync(login)
            : null;
    }


    private async Task ApplyRuntimeLockoutSettingsAsync()
    {
        var settings = await _db.SecuritySettings.AsNoTracking().FirstOrDefaultAsync();
        if (settings is null) return;

        _userManager.Options.Lockout.MaxFailedAccessAttempts = Math.Clamp(settings.MaxFailedAccessAttempts, 1, 25);
        _userManager.Options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(Math.Clamp(settings.LockoutMinutes, 1, 1440));
    }

    private static string NormalizeLoginInput(string? login)
    {
        return (login ?? string.Empty).Trim();
    }

    private async Task<string> BuildEmailConfirmationLinkAsync(ApplicationUser user, Func<Guid, string, string> buildUrl)
    {
        var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        return buildUrl(user.Id, EncodeToken(code));
    }

    private async Task<bool> TrySendEmailConfirmationAsync(string email, string confirmationLink)
    {
        try
        {
            await _emailSender.SendEmailConfirmationAsync(email, confirmationLink);
            return false;
        }
        catch (EmailSenderNotConfiguredException)
        {
            return true;
        }
    }

    private async Task<bool> TrySendPasswordResetAsync(string email, string resetLink)
    {
        try
        {
            await _emailSender.SendPasswordResetAsync(email, resetLink);
            return false;
        }
        catch (EmailSenderNotConfiguredException)
        {
            return true;
        }
    }

    private static string EncodeToken(string token)
    {
        return WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
    }

    private static string DecodeToken(string token)
    {
        return Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
    }

    private static IReadOnlyList<IdentityOperationError> ToErrors(IEnumerable<IdentityError> errors)
    {
        return errors.Select(x => new IdentityOperationError(x.Code, x.Description)).ToArray();
    }
}
