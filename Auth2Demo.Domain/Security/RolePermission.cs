using Auth2Demo.Domain.Common;

namespace Auth2Demo.Domain.Security;

public sealed class RolePermission : AuditableEntity<Guid>
{
    public Guid RoleId { get; set; }
    public Guid PermissionId { get; set; }
}
