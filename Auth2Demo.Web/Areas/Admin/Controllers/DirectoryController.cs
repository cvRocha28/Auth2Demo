using Auth2Demo.Application.Services.Admin;
using Auth2Demo.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Auth2Demo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = AuthPolicies.Admin)]
public sealed class DirectoryController : Controller
{
    private readonly ITenantGovernanceService _service;

    public DirectoryController(ITenantGovernanceService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> Index()
        => View(await _service.GetDirectoryOverviewAsync());

    [HttpGet]
    public async Task<IActionResult> Users(Guid? companyId, string? q)
    {
        await PopulateCompaniesAsync(companyId);
        ViewBag.Query = q;
        return View(await _service.SearchDirectoryUsersAsync(companyId, q));
    }

    [HttpGet]
    public async Task<IActionResult> Groups(Guid? companyId, string? q)
    {
        await PopulateCompaniesAsync(companyId);
        ViewBag.Query = q;
        return View(await _service.SearchDirectoryGroupsAsync(companyId, q));
    }

    [HttpGet]
    public async Task<IActionResult> Group(Guid id)
    {
        var model = await _service.GetGroupDetailsAsync(id);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateGroup(Guid id, string name, string? description, bool isEnabled)
    {
        try
        {
            await _service.UpdateGroupAsync(id, name, description, isEnabled);
            TempData["Success"] = "Group settings updated.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Group), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddGroupMember(Guid groupId, Guid userId)
    {
        try
        {
            await _service.AddGroupMemberAsync(groupId, userId);
            TempData["Success"] = "Member added to the group.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }
        return RedirectToAction(nameof(Group), new { id = groupId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveGroupMember(Guid groupId, Guid userId)
    {
        try
        {
            await _service.RemoveGroupMemberAsync(groupId, userId);
            TempData["Success"] = "Member removed from the group.";
        }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Group), new { id = groupId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteGroup(Guid id, Guid companyId)
    {
        try
        {
            await _service.DeleteGroupAsync(id);
            TempData["Success"] = "Group and its related assignments were deleted.";
        }
        catch (InvalidOperationException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Groups), new { companyId });
    }

    private async Task PopulateCompaniesAsync(Guid? selected)
    {
        var overview = await _service.GetDirectoryOverviewAsync();
        ViewBag.CompanyId = selected;
        ViewBag.Companies = overview.Companies
            .Select(x => new SelectListItem(x.Name, x.Id.ToString(), selected == x.Id))
            .ToList();
    }
}
