using Auth2Demo.Application.Services.Admin;
using Auth2Demo.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Auth2Demo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = AuthPolicies.Admin)]
public sealed class TenantDirectoryController : Controller
{
    private readonly ITenantGovernanceService _service;

    public TenantDirectoryController(ITenantGovernanceService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> Index(Guid companyId)
    {
        var model = await _service.GetCompanyDirectoryAsync(companyId);
        return model is null ? NotFound() : View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Users(Guid companyId)
    {
        var model = await _service.GetCompanyDirectoryAsync(companyId);
        return model is null ? NotFound() : View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Groups(Guid companyId)
    {
        var model = await _service.GetCompanyDirectoryAsync(companyId);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddUser(Guid companyId, Guid userId, string? department, string? jobTitle, bool isDefault = false)
    {
        try
        {
            await _service.AddUserToCompanyAsync(companyId, userId, department, jobTitle, isDefault);
            TempData["Success"] = "User membership created successfully.";
        }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Users), new { companyId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateUser(Guid companyId, Guid membershipId, bool isEnabled, bool isDefault, string? department, string? jobTitle)
    {
        try
        {
            await _service.UpdateCompanyUserAsync(companyId, membershipId, isEnabled, isDefault, department, jobTitle);
            TempData["Success"] = "Tenant membership updated successfully.";
        }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Users), new { companyId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveUser(Guid companyId, Guid membershipId)
    {
        try
        {
            await _service.RemoveUserFromCompanyAsync(companyId, membershipId);
            TempData["Success"] = "Tenant membership and related tenant access were removed.";
        }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Users), new { companyId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateGroup(Guid companyId, string name, string? description)
    {
        try
        {
            var groupId = await _service.CreateGroupAsync(companyId, name, description);
            TempData["Success"] = "Security group created successfully.";
            return RedirectToAction("Group", "Directory", new { id = groupId });
        }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Groups), new { companyId });
    }
}
