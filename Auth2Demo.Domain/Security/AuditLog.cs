using Auth2Demo.Domain.Common;

namespace Auth2Demo.Domain.Security;

public sealed class AuditLog : AuditableEntity<Guid>
{
    public string EventType { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Outcome { get; set; } = string.Empty;
    public Guid? UserId { get; set; }
    public string? UserEmail { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? Provider { get; set; }
    public string? Description { get; set; }
    public string? DataJson { get; set; }
}
