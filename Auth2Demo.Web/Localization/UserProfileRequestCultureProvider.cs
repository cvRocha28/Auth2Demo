using Auth2Demo.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;

namespace Auth2Demo.Web.Localization;

public sealed class UserProfileRequestCultureProvider : RequestCultureProvider
{
    private static readonly HashSet<string> SupportedCultures = new(StringComparer.OrdinalIgnoreCase)
    {
        "pt-BR",
        "en-US"
    };

    public override async Task<ProviderCultureResult?> DetermineProviderCultureResult(HttpContext httpContext)
    {
        if (httpContext.User.Identity?.IsAuthenticated != true)
        {
            return new ProviderCultureResult("en-US", "en-US");
        }

        var userManager = httpContext.RequestServices.GetService<UserManager<ApplicationUser>>();
        if (userManager is null)
        {
            return null;
        }

        var user = await userManager.GetUserAsync(httpContext.User);
        var locale = NormalizeLocale(user?.Locale);

        return new ProviderCultureResult(locale, locale);
    }

    public static string NormalizeLocale(string? locale)
    {
        if (string.IsNullOrWhiteSpace(locale))
        {
            return "en-US";
        }

        return SupportedCultures.Contains(locale) ? locale : "en-US";
    }
}
