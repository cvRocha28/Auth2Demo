using Auth2Demo.Application.Services.Admin;
using Auth2Demo.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Auth2Demo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = AuthPolicies.Admin)]
public sealed class EnterpriseApplicationsController : Controller
{
    private readonly IEnterpriseApplicationService _service;

    public EnterpriseApplicationsController(IEnterpriseApplicationService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        return View(await _service.ListAsync());
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var model = await _service.GetForEditAsync(id);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(SaveEnterpriseApplicationData model)
    {
        if (model.ApplicationId == Guid.Empty)
        {
            return BadRequest();
        }

        try
        {
            await _service.SaveAsync(model);
            TempData["Success"] = "Enterprise application configuration saved successfully.";
        }
        catch (InvalidOperationException exception)
        {
            TempData["Error"] = exception.Message;
        }

        return RedirectToAction(nameof(Edit), new { id = model.ApplicationId });
    }
}
