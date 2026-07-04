using Auth2Demo.Domain.Security;
using Auth2Demo.Web.Security;
using Auth2Demo.Infrastructure.Services.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Auth2Demo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = AuthPolicies.Admin)]
public sealed class EmailTemplatesController : Controller
{
    private readonly IAdminEmailTemplateService _templates;
    public EmailTemplatesController(IAdminEmailTemplateService templates) => _templates = templates;
    public async Task<IActionResult> Index() => View(await _templates.ListAsync());
    public async Task<IActionResult> Edit(Guid id) => View(await _templates.GetForEditAsync(id));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EmailTemplate model)
    {
        await _templates.SaveAsync(model);
        return RedirectToAction(nameof(Index));
    }
}
