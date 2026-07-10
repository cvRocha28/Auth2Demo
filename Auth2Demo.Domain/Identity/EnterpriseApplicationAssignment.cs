using Auth2Demo.Domain.Common;
namespace Auth2Demo.Domain.Identity;
public sealed class EnterpriseApplicationAssignment : AuditableEntity<Guid>
{
    private EnterpriseApplicationAssignment() { }
    private EnterpriseApplicationAssignment(Guid appId,Guid companyId,EnterpriseAssignmentType type,Guid? userId,Guid? groupId,Guid? roleId){Id=Guid.NewGuid();ApplicationId=appId;CompanyId=companyId;PrincipalType=type;UserId=userId;GroupId=groupId;ApplicationRoleId=roleId;IsEnabled=true;}
    public static EnterpriseApplicationAssignment ForUser(Guid appId,Guid companyId,Guid userId,Guid? roleId=null)=>new(appId,companyId,EnterpriseAssignmentType.User,userId,null,roleId);
    public static EnterpriseApplicationAssignment ForGroup(Guid appId,Guid companyId,Guid groupId,Guid? roleId=null)=>new(appId,companyId,EnterpriseAssignmentType.Group,null,groupId,roleId);
    public Guid ApplicationId { get; private set; }
    public Guid CompanyId { get; private set; }
    public EnterpriseAssignmentType PrincipalType { get; private set; }
    public Guid? UserId { get; private set; }
    public Guid? GroupId { get; private set; }
    public CompanyGroup? Group { get; private set; }
    public Guid? ApplicationRoleId { get; private set; }
    public EnterpriseApplicationRole? ApplicationRole { get; private set; }
    public bool IsEnabled { get; private set; }
    public void Update(Guid? roleId,bool enabled){ApplicationRoleId=roleId;IsEnabled=enabled;MarkAsUpdated();}
}
