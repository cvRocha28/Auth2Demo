using Auth2Demo.Domain.Common;
namespace Auth2Demo.Domain.Identity;
public sealed class CompanyGroup : AuditableEntity<Guid>
{
    private CompanyGroup() { }
    public CompanyGroup(Guid companyId, string name, string? description=null) { Id=Guid.NewGuid(); CompanyId=companyId; Name=name.Trim(); Description=Normalize(description); IsEnabled=true; }
    public Guid CompanyId { get; private set; }
    public Company? Company { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsEnabled { get; private set; }
    public void Update(string name, string? description, bool isEnabled) { Name=name.Trim(); Description=Normalize(description); IsEnabled=isEnabled; MarkAsUpdated(); }
    private static string? Normalize(string? value)=>string.IsNullOrWhiteSpace(value)?null:value.Trim();
}
