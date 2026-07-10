using Auth2Demo.Domain.Common;

namespace Auth2Demo.Domain.Identity;

/// <summary>
/// Represents an Enterprise Application assignment: an application owned by one company
/// can be made available to another company/tenant, similar to Entra Enterprise Applications.
/// </summary>
public sealed class ApplicationTenantAssignment : AuditableEntity<Guid>
{
    private ApplicationTenantAssignment() { }

    public ApplicationTenantAssignment(Guid applicationId, Guid companyId)
    {
        Id = Guid.NewGuid();
        ApplicationId = applicationId;
        CompanyId = companyId;
        IsEnabled = true;
        RequireUserAssignment = false;
    }

    public Guid ApplicationId { get; private set; }
    public Guid CompanyId { get; private set; }
    public Company? Company { get; private set; }
    public bool IsEnabled { get; private set; }
    public bool RequireUserAssignment { get; private set; }
    public string? Notes { get; private set; }

    public void Update(bool isEnabled, bool requireUserAssignment, string? notes)
    {
        IsEnabled = isEnabled;
        RequireUserAssignment = requireUserAssignment;
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        MarkAsUpdated();
    }
}
