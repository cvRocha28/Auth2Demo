using Auth2Demo.Web.Security;
using Auth2Demo.Infrastructure.Services.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Auth2Demo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = AuthPolicies.Admin)]
public sealed class HealthController : Controller
{
    private readonly IAdminHealthService _health;
    public HealthController(IAdminHealthService health) => _health = health;
    public async Task<IActionResult> Index() => View(await _health.GetAsync());
}
