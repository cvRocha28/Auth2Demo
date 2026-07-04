using Auth2Demo.Domain.Common;

namespace Auth2Demo.Domain.Security;

public sealed class BrandingSettings : AuditableEntity<Guid>
{
    public string TenantName { get; set; } = "Auth2Demo";
    public string? LogoUrl { get; set; }
    public string? FaviconUrl { get; set; }
    public string PrimaryColor { get; set; } = "#2563eb";
    public string SecondaryColor { get; set; } = "#0f172a";
    public string Theme { get; set; } = "Light";
    public string? CustomCss { get; set; }
}
