using System.ComponentModel.DataAnnotations;

namespace Auth2Demo.Web.Areas.Admin.Models;

public sealed class ClientBrandingViewModel
{
    [Required]
    public string ClientId { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public bool IsEnabled { get; set; }

    [StringLength(120)]
    public string? TenantName { get; set; }

    [Url]
    [StringLength(500)]
    public string? LogoUrl { get; set; }

    [Url]
    [StringLength(500)]
    public string? FaviconUrl { get; set; }

    [RegularExpression("^#[0-9a-fA-F]{6}$", ErrorMessage = "Use a valid hexadecimal color, for example #2563eb.")]
    public string PrimaryColor { get; set; } = "#2563eb";

    [RegularExpression("^#[0-9a-fA-F]{6}$", ErrorMessage = "Use a valid hexadecimal color, for example #0f172a.")]
    public string SecondaryColor { get; set; } = "#0f172a";

    [RegularExpression("^#[0-9a-fA-F]{6}$", ErrorMessage = "Use a valid hexadecimal color, for example #eff6ff.")]
    public string BackgroundColor { get; set; } = "#eff6ff";

    [RegularExpression("^#[0-9a-fA-F]{6}$", ErrorMessage = "Use a valid hexadecimal color, for example #ffffff.")]
    public string SurfaceColor { get; set; } = "#ffffff";

    [RegularExpression("^#[0-9a-fA-F]{6}$", ErrorMessage = "Use a valid hexadecimal color, for example #111827.")]
    public string TextColor { get; set; } = "#111827";

    [RegularExpression("^#[0-9a-fA-F]{6}$", ErrorMessage = "Use a valid hexadecimal color, for example #64748b.")]
    public string MutedTextColor { get; set; } = "#64748b";

    [RegularExpression("^#[0-9a-fA-F]{6}$", ErrorMessage = "Use a valid hexadecimal color, for example #cbd5e1.")]
    public string BorderColor { get; set; } = "#cbd5e1";

    [RegularExpression("^#[0-9a-fA-F]{6}$", ErrorMessage = "Use a valid hexadecimal color, for example #16a34a.")]
    public string SuccessColor { get; set; } = "#16a34a";

    [RegularExpression("^#[0-9a-fA-F]{6}$", ErrorMessage = "Use a valid hexadecimal color, for example #f59e0b.")]
    public string WarningColor { get; set; } = "#f59e0b";

    [RegularExpression("^#[0-9a-fA-F]{6}$", ErrorMessage = "Use a valid hexadecimal color, for example #dc2626.")]
    public string DangerColor { get; set; } = "#dc2626";

    [Range(8, 48)]
    public int CardRadius { get; set; } = 28;

    [Range(6, 32)]
    public int ButtonRadius { get; set; } = 14;

    public bool UseGradientButtons { get; set; } = true;

    public bool ShowCreateAccountLink { get; set; } = true;

    public bool ShowForgotPasswordLink { get; set; } = true;

    public bool EnableLocalLogin { get; set; } = true;

    public List<string> EnabledProviderSchemes { get; set; } = new();

    public List<ClientBrandingProviderOptionViewModel> ProviderOptions { get; set; } = new();

    public string Theme { get; set; } = "Light";

    public string? CustomCss { get; set; }
}


public sealed class ClientBrandingProviderOptionViewModel
{
    public string Scheme { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string ButtonText { get; init; } = string.Empty;
    public bool IsConfigured { get; init; }
    public int SortOrder { get; init; }
}
