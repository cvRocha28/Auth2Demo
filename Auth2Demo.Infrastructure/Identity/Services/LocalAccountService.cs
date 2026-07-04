using System.Text;
using Auth2Demo.Application.Services.Identity;
using Auth2Demo.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;

namespace Auth2Demo.Infrastructure.Identity.Services;

public sealed class LocalAccountService : ILocalAccountService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IIdentityEmailSender _emailSender;

    public LocalAccountService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IIdentityEmailSender emailSender)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _emailSender = emailSender;
    }

    public async Task<RegisterLocalAccountResult> RegisterAsync(RegisterLocalAccountRequest request)
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = request.Email,
            Email = request.Email,
            DisplayName = request.DisplayName,
            Status = UserStatus.Active,
            EmailConfirmed = false
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            return new RegisterLocalAccountResult
            {
                Email = request.Email,
                Errors = ToErrors(result.Errors)
            };
        }

        var confirmationLink = await BuildEmailConfirmationLinkAsync(user, request.BuildEmailConfirmationUrl);
        var emailDeliveryUnavailable = await TrySendEmailConfirmationAsync(user.Email ?? request.Email, confirmationLink);

        return new RegisterLocalAccountResult
        {
            Succeeded = true,
            UserId = user.Id,
            Email = user.Email ?? request.Email,
            ConfirmationLink = confirmationLink,
            EmailDeliveryUnavailable = emailDeliveryUnavailable
        };
    }

    public async Task<LocalLoginResult> PasswordSignInAsync(LoginLocalAccountRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            return new LocalLoginResult { Status = LocalLoginStatus.InvalidCredentials, Email = request.Email };
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
            var emailDeliveryUnavailable = await TrySendEmailConfirmationAsync(user.Email ?? request.Email, confirmationLink);

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
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return new EmailLinkResult { Email = email };
        }

        if (await _userManager.IsEmailConfirmedAsync(user))
        {
            return new EmailLinkResult { Succeeded = true, UserFound = true, Email = user.Email ?? email };
        }

        var link = await BuildEmailConfirmationLinkAsync(user, buildUrl);
        var emailDeliveryUnavailable = await TrySendEmailConfirmationAsync(user.Email ?? email, link);

        return new EmailLinkResult
        {
            Succeeded = true,
            UserFound = true,
            Email = user.Email ?? email,
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
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null || !await _userManager.HasPasswordAsync(user))
        {
            return new EmailLinkResult { Succeeded = true, Email = request.Email };
        }

        var code = await _userManager.GeneratePasswordResetTokenAsync(user);
        var encodedCode = EncodeToken(code);
        var resetLink = request.BuildPasswordResetUrl(user.Email ?? request.Email, encodedCode);
        var emailDeliveryUnavailable = await TrySendPasswordResetAsync(user.Email ?? request.Email, resetLink);

        return new EmailLinkResult
        {
            Succeeded = true,
            UserFound = true,
            Email = user.Email ?? request.Email,
            Link = resetLink,
            EmailDeliveryUnavailable = emailDeliveryUnavailable
        };
    }

    public async Task<ResetPasswordResult> ResetPasswordAsync(ResetPasswordRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
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
