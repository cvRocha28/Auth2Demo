using Auth2Demo.Domain.Common;
namespace Auth2Demo.Domain.Identity;
public sealed class EnterpriseApplicationRole : AuditableEntity<Guid>
{
    private EnterpriseApplicationRole() { }
    public EnterpriseApplicationRole(Guid applicationId,string name,string value,string? description=null){Id=Guid.NewGuid();ApplicationId=applicationId;Name=name.Trim();Value=value.Trim();Description=Normalize(description);IsEnabled=true;}
    public Guid ApplicationId { get; private set; }
    public string Name { get; private set; }=string.Empty;
    public string Value { get; private set; }=string.Empty;
    public string? Description { get; private set; }
    public bool IsEnabled { get; private set; }
    public void Update(string name,string value,string? description,bool enabled){Name=name.Trim();Value=value.Trim();Description=Normalize(description);IsEnabled=enabled;MarkAsUpdated();}
    private static string? Normalize(string? value)=>string.IsNullOrWhiteSpace(value)?null:value.Trim();
}
