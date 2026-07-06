using Auth2Demo.Application.Services.Admin;
using Auth2Demo.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Auth2Demo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = AuthPolicies.Admin)]
public sealed class DashboardController : Controller
{
    private readonly IAdminDashboardService _dashboard;
    public DashboardController(IAdminDashboardService dashboard) => _dashboard = dashboard;

    public async Task<IActionResult> Index()
    {
        var data = await _dashboard.GetAsync();
        return View(data);
    }
}
