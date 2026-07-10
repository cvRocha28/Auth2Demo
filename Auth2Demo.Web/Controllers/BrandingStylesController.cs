using System.Globalization;
using System.Text;
using Auth2Demo.Web.Services.Branding;
using Microsoft.AspNetCore.Mvc;

namespace Auth2Demo.Web.Controllers;

[ApiExplorerSettings(IgnoreApi = true)]
public sealed class BrandingStylesController : Controller
{
    private readonly IBrandingResolver _brandingResolver;

    public BrandingStylesController(IBrandingResolver brandingResolver)
    {
        _brandingResolver = brandingResolver;
    }

    [HttpGet("/branding/theme.css")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> Theme(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var branding = await _brandingResolver.ResolveAsync(HttpContext);

        var css = new StringBuilder();
        css.AppendLine(":root {");
        css.AppendLine($"  --primary: {branding.PrimaryColor};");
        css.AppendLine($"  --primary-2: {branding.SecondaryColor};");
        css.AppendLine($"  --primary-soft: color-mix(in srgb, {branding.PrimaryColor} 14%, white);");
        css.AppendLine($"  --secondary-brand: {branding.SecondaryColor};");
        css.AppendLine($"  --bg: {branding.BackgroundColor};");
        css.AppendLine($"  --surface: {branding.SurfaceColor};");
        css.AppendLine($"  --surface-2: color-mix(in srgb, {branding.SurfaceColor} 90%, {branding.BackgroundColor});");
        css.AppendLine($"  --text: {branding.TextColor};");
        css.AppendLine($"  --muted: {branding.MutedTextColor};");
        css.AppendLine($"  --line: {branding.BorderColor};");
        css.AppendLine($"  --success: {branding.SuccessColor};");
        css.AppendLine($"  --warning: {branding.WarningColor};");
        css.AppendLine($"  --danger: {branding.DangerColor};");
        css.AppendLine($"  --auth-card-radius: {branding.CardRadius.ToString(CultureInfo.InvariantCulture)}px;");
        css.AppendLine($"  --auth-button-radius: {branding.ButtonRadius.ToString(CultureInfo.InvariantCulture)}px;");
        css.AppendLine($"  --auth-button-background: {(branding.UseGradientButtons ? $"linear-gradient(135deg, {branding.PrimaryColor}, {branding.SecondaryColor})" : branding.PrimaryColor)};");
        css.AppendLine("}");

        if (branding.IsDark)
        {
            css.AppendLine(":root { --bg:#020617; --surface:#0f172a; --surface-2:#111827; --text:#e5e7eb; --muted:#94a3b8; --line:#1f2937; --shadow:0 18px 45px rgba(0,0,0,.35); }");
            css.AppendLine($"body {{ background:radial-gradient(circle at top left, color-mix(in srgb, {branding.PrimaryColor} 26%, #020617) 0, #020617 44%, #0f172a 100%); }}");
            css.AppendLine(".topbar { background:rgba(15,23,42,.88); }");
            css.AppendLine(".nav-link,.link-button { color:#cbd5e1; }");
        }

        if (!string.IsNullOrWhiteSpace(branding.CustomCss))
        {
            css.AppendLine(branding.CustomCss);
        }

        Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
        return Content(css.ToString(), "text/css; charset=utf-8", Encoding.UTF8);
    }
}
