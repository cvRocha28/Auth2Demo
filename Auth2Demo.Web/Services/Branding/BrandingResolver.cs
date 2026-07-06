using System.Text.Json;
using Auth2Demo.Application.Services.Admin;
using Auth2Demo.Domain.Security;
using Auth2Demo.Web.Models.Branding;
using Microsoft.AspNetCore.WebUtilities;
using OpenIddict.Abstractions;

namespace Auth2Demo.Web.Services.Branding;

public sealed class BrandingResolver : IBrandingResolver
{
    private const string BrandingPropertyName = "auth2demo:branding";
    private readonly IAdminBrandingService _globalBranding;
    private readonly IOpenIddictApplicationManager _applicationManager;

    public BrandingResolver(IAdminBrandingService globalBranding, IOpenIddictApplicationManager applicationManager)
    {
        _globalBranding = globalBranding;
        _applicationManager = applicationManager;
    }

    public async Task<BrandingViewModel> ResolveAsync(HttpContext httpContext)
    {
        var global = FromSettings(await _globalBranding.GetAsync());
        var clientId = ExtractClientId(httpContext);

        if (string.IsNullOrWhiteSpace(clientId))
        {
            return global;
        }

        var application = await _applicationManager.FindByClientIdAsync(clientId);
        if (application is null)
        {
            return global;
        }

        var clientBranding = ExtractClientBranding(application);
        return Merge(global, clientBranding);
    }

    public BrandingViewModel FromSettings(BrandingSettings settings) => new()
    {
        TenantName = Normalize(settings.TenantName, "Auth2Demo"),
        LogoUrl = NormalizeNullable(settings.LogoUrl),
        FaviconUrl = NormalizeNullable(settings.FaviconUrl),
        PrimaryColor = NormalizeColor(settings.PrimaryColor, "#2563eb"),
        SecondaryColor = NormalizeColor(settings.SecondaryColor, "#0f172a"),
        BackgroundColor = "#f5f7fb",
        SurfaceColor = "#ffffff",
        TextColor = "#111827",
        MutedTextColor = "#6b7280",
        BorderColor = "#e5e7eb",
        SuccessColor = "#22c55e",
        WarningColor = "#f7b500",
        DangerColor = "#ef4444",
        CardRadius = 32,
        ButtonRadius = 16,
        UseGradientButtons = true,
        ShowCreateAccountLink = true,
        ShowForgotPasswordLink = true,
        EnableLocalLogin = true,
        EnabledProviderSchemes = Array.Empty<string>(),
        RestrictExternalProviders = false,
        Theme = NormalizeTheme(settings.Theme),
        CustomCss = NormalizeNullable(settings.CustomCss)
    };

    private static BrandingViewModel Merge(BrandingViewModel global, ClientBrandingData? client)
    {
        if (client is null || !client.IsEnabled)
        {
            return global;
        }

        return new BrandingViewModel
        {
            TenantName = Normalize(client.TenantName, global.TenantName),
            LogoUrl = NormalizeNullable(client.LogoUrl) ?? global.LogoUrl,
            FaviconUrl = NormalizeNullable(client.FaviconUrl) ?? global.FaviconUrl,
            PrimaryColor = NormalizeColor(client.PrimaryColor, global.PrimaryColor),
            SecondaryColor = NormalizeColor(client.SecondaryColor, global.SecondaryColor),
            BackgroundColor = NormalizeColor(client.BackgroundColor, global.BackgroundColor),
            SurfaceColor = NormalizeColor(client.SurfaceColor, global.SurfaceColor),
            TextColor = NormalizeColor(client.TextColor, global.TextColor),
            MutedTextColor = NormalizeColor(client.MutedTextColor, global.MutedTextColor),
            BorderColor = NormalizeColor(client.BorderColor, global.BorderColor),
            SuccessColor = NormalizeColor(client.SuccessColor, global.SuccessColor),
            WarningColor = NormalizeColor(client.WarningColor, global.WarningColor),
            DangerColor = NormalizeColor(client.DangerColor, global.DangerColor),
            CardRadius = NormalizeRange(client.CardRadius, global.CardRadius, 8, 48),
            ButtonRadius = NormalizeRange(client.ButtonRadius, global.ButtonRadius, 6, 32),
            UseGradientButtons = client.UseGradientButtons ?? global.UseGradientButtons,
            ShowCreateAccountLink = client.ShowCreateAccountLink ?? global.ShowCreateAccountLink,
            ShowForgotPasswordLink = client.ShowForgotPasswordLink ?? global.ShowForgotPasswordLink,
            EnableLocalLogin = client.EnableLocalLogin ?? global.EnableLocalLogin,
            EnabledProviderSchemes = client.EnabledProviderSchemes?.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray() ?? Array.Empty<string>(),
            RestrictExternalProviders = true,
            Theme = NormalizeTheme(string.IsNullOrWhiteSpace(client.Theme) ? global.Theme : client.Theme),
            CustomCss = NormalizeNullable(client.CustomCss) ?? global.CustomCss
        };
    }

    private static string? ExtractClientId(HttpContext httpContext)
    {
        var direct = httpContext.Request.Query[OpenIddictConstants.Parameters.ClientId].ToString();
        if (!string.IsNullOrWhiteSpace(direct))
        {
            return direct;
        }

        var returnUrl = httpContext.Request.Query["returnUrl"].ToString();
        if (string.IsNullOrWhiteSpace(returnUrl) && httpContext.Request.HasFormContentType)
        {
            returnUrl = httpContext.Request.Form["ReturnUrl"].ToString();
            if (string.IsNullOrWhiteSpace(returnUrl))
            {
                returnUrl = httpContext.Request.Form["returnUrl"].ToString();
            }
        }

        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            return null;
        }

        var queryIndex = returnUrl.IndexOf('?', StringComparison.Ordinal);
        if (queryIndex < 0 || queryIndex == returnUrl.Length - 1)
        {
            return null;
        }

        var query = QueryHelpers.ParseQuery(returnUrl[(queryIndex + 1)..]);
        return query.TryGetValue(OpenIddictConstants.Parameters.ClientId, out var clientId)
            ? clientId.ToString()
            : null;
    }

    private static ClientBrandingData? ExtractClientBranding(object application)
    {
        var property = application.GetType().GetProperty("Properties")?.GetValue(application)?.ToString();
        if (string.IsNullOrWhiteSpace(property))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(property);
            if (!document.RootElement.TryGetProperty(BrandingPropertyName, out var value))
            {
                return null;
            }

            return JsonSerializer.Deserialize<ClientBrandingData>(value.GetRawText(), JsonOptions());
        }
        catch
        {
            return null;
        }
    }

    private static JsonSerializerOptions JsonOptions() => new(JsonSerializerDefaults.Web);

    private static string Normalize(string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();

    private static string? NormalizeNullable(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string NormalizeTheme(string? value) =>
        string.Equals(value, "Dark", StringComparison.OrdinalIgnoreCase) ? "Dark" : "Light";

    private static string NormalizeColor(string? value, string fallback) =>
        !string.IsNullOrWhiteSpace(value) && value.Trim().StartsWith('#') ? value.Trim() : fallback;

    private static int NormalizeRange(int? value, int fallback, int min, int max) =>
        value.HasValue && value.Value >= min && value.Value <= max ? value.Value : fallback;

    private sealed class ClientBrandingData
    {
        public bool IsEnabled { get; set; }
        public string? TenantName { get; set; }
        public string? LogoUrl { get; set; }
        public string? FaviconUrl { get; set; }
        public string? PrimaryColor { get; set; }
        public string? SecondaryColor { get; set; }
        public string? BackgroundColor { get; set; }
        public string? SurfaceColor { get; set; }
        public string? TextColor { get; set; }
        public string? MutedTextColor { get; set; }
        public string? BorderColor { get; set; }
        public string? SuccessColor { get; set; }
        public string? WarningColor { get; set; }
        public string? DangerColor { get; set; }
        public int? CardRadius { get; set; }
        public int? ButtonRadius { get; set; }
        public bool? UseGradientButtons { get; set; }
        public bool? ShowCreateAccountLink { get; set; }
        public bool? ShowForgotPasswordLink { get; set; }
        public bool? EnableLocalLogin { get; set; }
        public string[]? EnabledProviderSchemes { get; set; }
        public string? Theme { get; set; }
        public string? CustomCss { get; set; }
    }
}
