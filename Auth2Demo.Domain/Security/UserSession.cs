using Auth2Demo.Domain.Common;

namespace Auth2Demo.Domain.Security;

public sealed class UserSession : AuditableEntity<Guid>
{
    public Guid UserId { get; set; }
    public string UserEmail { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? DeviceName { get; set; }
    public string? Location { get; set; }
    public DateTimeOffset LastSeenAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? RevokedAt { get; set; }
    public bool IsRevoked { get; set; }
}
