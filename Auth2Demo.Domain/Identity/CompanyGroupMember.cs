using Auth2Demo.Domain.Common;
namespace Auth2Demo.Domain.Identity;
public sealed class CompanyGroupMember : AuditableEntity<Guid>
{
    private CompanyGroupMember() { }
    public CompanyGroupMember(Guid groupId, Guid userId) { Id=Guid.NewGuid(); GroupId=groupId; UserId=userId; }
    public Guid GroupId { get; private set; }
    public CompanyGroup? Group { get; private set; }
    public Guid UserId { get; private set; }
}
