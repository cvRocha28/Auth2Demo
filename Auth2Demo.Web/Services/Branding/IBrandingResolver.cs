using Auth2Demo.Domain.Security;
using Auth2Demo.Web.Models.Branding;

namespace Auth2Demo.Web.Services.Branding;

public interface IBrandingResolver
{
    Task<BrandingViewModel> ResolveAsync(HttpContext httpContext);
    BrandingViewModel FromSettings(BrandingSettings settings);
}
