using Auth2Demo.Domain.Common;

namespace Auth2Demo.Domain.Security;

public sealed class EmailTemplate : AuditableEntity<Guid>
{
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string BodyHtml { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
}
