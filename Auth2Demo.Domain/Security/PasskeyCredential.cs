using Auth2Demo.Domain.Common;

namespace Auth2Demo.Domain.Security;

public sealed class PasskeyCredential : AuditableEntity<Guid>
{
    public Guid UserId { get; set; }
    public string UserEmail { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string CredentialId { get; set; } = string.Empty;
    public string? DeviceType { get; set; }
    public DateTimeOffset? LastUsedAt { get; set; }
}
