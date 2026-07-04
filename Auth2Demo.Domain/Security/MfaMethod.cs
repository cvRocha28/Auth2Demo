using Auth2Demo.Domain.Common;

namespace Auth2Demo.Domain.Security;

public sealed class MfaMethod : AuditableEntity<Guid>
{
    public Guid UserId { get; set; }
    public string UserEmail { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public bool IsDefault { get; set; }
    public DateTimeOffset? LastUsedAt { get; set; }
}
