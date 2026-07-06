using Auth2Demo.Application.Services.Admin;
using Auth2Demo.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Auth2Demo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = AuthPolicies.Admin)]
public sealed class PasskeysController : Controller
{
    private readonly IAdminPasskeyService _passkeys;
    public PasskeysController(IAdminPasskeyService passkeys) => _passkeys = passkeys;
    public async Task<IActionResult> Index() => View(await _passkeys.ListAsync());
}
