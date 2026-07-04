namespace Auth2Demo.Application.Services.Identity;

public interface IIdentityEmailSender
{
    Task SendEmailConfirmationAsync(string email, string confirmationLink, CancellationToken cancellationToken = default);
    Task SendPasswordResetAsync(string email, string resetLink, CancellationToken cancellationToken = default);
}

public sealed class EmailSenderNotConfiguredException : InvalidOperationException
{
    public EmailSenderNotConfiguredException()
        : base("No e-mail sender was configured for Auth2Demo. The generated link must be shown on screen until SMTP or another provider is configured.")
    {
    }
}
