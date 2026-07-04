using Auth2Demo.Web.Security;
using Auth2Demo.Infrastructure.Services.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Auth2Demo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = AuthPolicies.Admin)]
public sealed class PermissionsController : Controller
{
    private readonly IAdminPermissionService _permissions;
    public PermissionsController(IAdminPermissionService permissions) => _permissions = permissions;

    public async Task<IActionResult> Index()
    {
        var data = await _permissions.GetIndexAsync();
        return View(data);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string name, string displayName, string category, string? description)
    {
        await _permissions.CreateAsync(name, displayName, category, description);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(Guid roleId, Guid permissionId)
    {
        await _permissions.ToggleAsync(roleId, permissionId);
        return RedirectToAction(nameof(Index));
    }
}
