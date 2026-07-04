using Auth2Demo.Web.Security;
using Auth2Demo.Infrastructure.Services.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Auth2Demo.Web;

namespace Auth2Demo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = AuthPolicies.Admin)]
public sealed class RolesController : Controller
{
    private readonly IAdminRoleService _roles;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public RolesController(
        IAdminRoleService roles,
        IStringLocalizer<SharedResource> localizer)
    {
        _roles = roles;
        _localizer = localizer;
    }

    public async Task<IActionResult> Index() => View(await _roles.ListAsync());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string name, string? description)
    {
        var result = await _roles.CreateAsync(name, description);
        TempData[result.Success ? "Success" : "Error"] = _localizer[result.Message].Value;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _roles.DeleteAsync(id);
        if (result.NotFound) return NotFound();
        TempData[result.Success ? "Success" : "Error"] = _localizer[result.Message].Value;
        return RedirectToAction(nameof(Index));
    }
}
