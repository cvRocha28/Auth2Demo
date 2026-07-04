using Auth2Demo.Application.Services.Identity;

namespace Auth2Demo.Infrastructure.Identity.Email;

public sealed class NullIdentityEmailSender : IIdentityEmailSender
{
    public Task SendEmailConfirmationAsync(string email, string confirmationLink, CancellationToken cancellationToken = default)
    {
        throw new EmailSenderNotConfiguredException();
    }

    public Task SendPasswordResetAsync(string email, string resetLink, CancellationToken cancellationToken = default)
    {
        throw new EmailSenderNotConfiguredException();
    }
}
