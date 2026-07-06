namespace Auth2Demo.Web.Models.Branding;

public sealed class BrandingViewModel
{
    public string TenantName { get; set; } = "Auth2Demo";
    public string? LogoUrl { get; set; }
    public string? FaviconUrl { get; set; }
    public string PrimaryColor { get; set; } = "#2563eb";
    public string SecondaryColor { get; set; } = "#0f172a";
    public string BackgroundColor { get; set; } = "#f5f7fb";
    public string SurfaceColor { get; set; } = "#ffffff";
    public string TextColor { get; set; } = "#111827";
    public string MutedTextColor { get; set; } = "#6b7280";
    public string BorderColor { get; set; } = "#e5e7eb";
    public string SuccessColor { get; set; } = "#22c55e";
    public string WarningColor { get; set; } = "#f7b500";
    public string DangerColor { get; set; } = "#ef4444";
    public int CardRadius { get; set; } = 32;
    public int ButtonRadius { get; set; } = 16;
    public bool UseGradientButtons { get; set; } = true;
    public bool ShowCreateAccountLink { get; set; } = true;
    public bool ShowForgotPasswordLink { get; set; } = true;
    public bool EnableLocalLogin { get; set; } = true;
    public IReadOnlyList<string> EnabledProviderSchemes { get; set; } = Array.Empty<string>();
    public bool RestrictExternalProviders { get; set; }
    public string Theme { get; set; } = "Light";
    public string? CustomCss { get; set; }

    public bool HasLogo => !string.IsNullOrWhiteSpace(LogoUrl);
    public bool IsDark => string.Equals(Theme, "Dark", StringComparison.OrdinalIgnoreCase);
}
