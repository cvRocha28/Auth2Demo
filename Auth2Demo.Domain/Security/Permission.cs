using Auth2Demo.Domain.Common;

namespace Auth2Demo.Domain.Security;

public sealed class Permission : AuditableEntity<Guid>
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
}
