using Auth2Demo.Domain.Common;

namespace Auth2Demo.Domain.Identity;

public sealed class CompanyUser : AuditableEntity<Guid>
{
    private CompanyUser() { }
    public CompanyUser(Guid companyId, Guid userId, bool isDefault = false)
    {
        Id = Guid.NewGuid(); CompanyId = companyId; UserId = userId; IsDefault = isDefault; IsEnabled = true;
    }
    public Guid CompanyId { get; private set; }
    public Guid UserId { get; private set; }
    public bool IsDefault { get; private set; }
    public bool IsEnabled { get; private set; }
    public string? Department { get; private set; }
    public string? JobTitle { get; private set; }
    public void Update(bool isEnabled, bool isDefault, string? department, string? jobTitle)
    { IsEnabled=isEnabled; IsDefault=isDefault; Department=Normalize(department); JobTitle=Normalize(jobTitle); MarkAsUpdated(); }
    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
