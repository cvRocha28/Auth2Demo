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
public sealed class SecuritySettingsController : Controller
{
    private readonly IAdminSecuritySettingsService _settings;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public SecuritySettingsController(
        IAdminSecuritySettingsService settings,
        IStringLocalizer<SharedResource> localizer)
    {
        _settings = settings;
        _localizer = localizer;
    }
    public async Task<IActionResult> Index() => View(await _settings.GetAsync());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(SecuritySettings model)
    {
        await _settings.SaveAsync(model);
        TempData["Message"] = _localizer["SettingsSaved"].Value;
        return RedirectToAction(nameof(Index));
    }
}
