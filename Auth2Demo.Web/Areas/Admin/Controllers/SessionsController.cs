using Auth2Demo.Application.Services.Admin;
using Auth2Demo.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Auth2Demo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = AuthPolicies.Admin)]
public sealed class SessionsController : Controller
{
    private readonly IAdminSessionService _sessions;
    public SessionsController(IAdminSessionService sessions) => _sessions = sessions;

    public async Task<IActionResult> Index() => View(await _sessions.ListAsync());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Revoke(Guid id)
    {
        await _sessions.RevokeAsync(id);
        return RedirectToAction(nameof(Index));
    }
}
