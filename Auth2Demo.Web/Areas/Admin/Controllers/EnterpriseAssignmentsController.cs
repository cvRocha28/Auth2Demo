using Auth2Demo.Application.Services.Admin;
using Auth2Demo.Domain.Identity;
using Auth2Demo.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Auth2Demo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = AuthPolicies.Admin)]
public sealed class EnterpriseAssignmentsController : Controller
{
    private readonly ITenantGovernanceService _service;
    public EnterpriseAssignmentsController(ITenantGovernanceService service)=>_service=service;
    [HttpGet]
    public async Task<IActionResult> Index(Guid applicationId)
    {var model=await _service.GetAssignmentsAsync(applicationId);return model is null?NotFound():View(model);}
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Assign(Guid applicationId,Guid companyId,EnterpriseAssignmentType principalType,Guid principalId,Guid? roleId)
    {try{await _service.AssignAsync(applicationId,companyId,principalType,principalId,roleId);TempData["Success"]="Assignment created.";}catch(InvalidOperationException ex){TempData["Error"]=ex.Message;}return RedirectToAction(nameof(Index),new{applicationId});}
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(Guid applicationId,Guid assignmentId)
    {await _service.RemoveAssignmentAsync(assignmentId);TempData["Success"]="Assignment removed.";return RedirectToAction(nameof(Index),new{applicationId});}
    [HttpPost,ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveRole(Guid applicationId,Guid? roleId,string name,string value,string? description,bool enabled=true)
    {try{await _service.SaveRoleAsync(applicationId,roleId,name,value,description,enabled);TempData["Success"]="Application role saved.";}catch(InvalidOperationException ex){TempData["Error"]=ex.Message;}return RedirectToAction(nameof(Index),new{applicationId});}
}
