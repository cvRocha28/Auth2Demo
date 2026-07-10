using Auth2Demo.Domain.Common;

namespace Auth2Demo.Domain.Identity;

/// <summary>
/// Explicit list of identity providers allowed for an application.
/// This lets Portal Atento accept Microsoft Atento and Microsoft Interfile, for example.
/// </summary>
public sealed class ApplicationIdentityProvider : AuditableEntity<Guid>
{
    private ApplicationIdentityProvider() { }

    public ApplicationIdentityProvider(Guid applicationId, Guid identityProviderId)
    {
        Id = Guid.NewGuid();
        ApplicationId = applicationId;
        IdentityProviderId = identityProviderId;
        IsEnabled = true;
    }

    public Guid ApplicationId { get; private set; }
    public Guid IdentityProviderId { get; private set; }
    public IdentityProvider? IdentityProvider { get; private set; }
    public bool IsEnabled { get; private set; }

    public void SetEnabled(bool isEnabled)
    {
        IsEnabled = isEnabled;
        MarkAsUpdated();
    }
}
