using Auth2Demo.Web.Security;
using Auth2Demo.Infrastructure.Services.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Auth2Demo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = AuthPolicies.Admin)]
public sealed class DevicesController : Controller
{
    private readonly IAdminDeviceService _devices;
    public DevicesController(IAdminDeviceService devices) => _devices = devices;
    public async Task<IActionResult> Index() => View(await _devices.ListAsync());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleTrusted(Guid id)
    {
        await _devices.ToggleTrustedAsync(id);
        return RedirectToAction(nameof(Index));
    }
}
