using Auth2Demo.Domain.Security;
using Auth2Demo.Web.Security;
using Auth2Demo.Infrastructure.Services.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Auth2Demo.Web;

namespace Auth2Demo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = AuthPolicies.Admin)]
public sealed class BrandingController : Controller
{
    private readonly IAdminBrandingService _branding;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public BrandingController(
        IAdminBrandingService branding,
        IStringLocalizer<SharedResource> localizer)
    {
        _branding = branding;
        _localizer = localizer;
    }
    public async Task<IActionResult> Index() => View(await _branding.GetAsync());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(BrandingSettings model)
    {
        await _branding.SaveAsync(model);
        TempData["Message"] = _localizer["BrandingSaved"].Value;
        return RedirectToAction(nameof(Index));
    }
}
