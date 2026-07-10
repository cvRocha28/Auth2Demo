using Auth2Demo.Domain.Common;

namespace Auth2Demo.Domain.Identity;

/// <summary>
/// Optional user assignment for an application, similar to Entra user/group assignment.
/// Keep it disabled by default and enable per application when needed.
/// </summary>
public sealed class ApplicationUserAssignment : AuditableEntity<Guid>
{
    private ApplicationUserAssignment() { }

    public ApplicationUserAssignment(Guid applicationId, Guid userId, string? role = null)
    {
        Id = Guid.NewGuid();
        ApplicationId = applicationId;
        UserId = userId;
        Role = role;
        IsEnabled = true;
    }

    public Guid ApplicationId { get; private set; }
    public Guid UserId { get; private set; }
    public string? Role { get; private set; }
    public bool IsEnabled { get; private set; }

    public void Update(string? role, bool isEnabled)
    {
        Role = string.IsNullOrWhiteSpace(role) ? null : role.Trim();
        IsEnabled = isEnabled;
        MarkAsUpdated();
    }
}
