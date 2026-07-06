using Auth2Demo.Application.Services.Admin;
using Auth2Demo.Web.Areas.Admin.Models;
using Auth2Demo.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Auth2Demo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = AuthPolicies.ClientManager)]
public sealed class ApplicationAuditController : Controller
{
    private readonly IApplicationAuditService _applications;

    public ApplicationAuditController(IApplicationAuditService applications)
    {
        _applications = applications;
    }

    public async Task<IActionResult> Index(bool includeDeleted = true)
    {
        var applications = await _applications.ListAsync(includeDeleted);

        ViewData["IncludeDeleted"] = includeDeleted;
        return View(applications.Select(application => new ApplicationAuditListItemViewModel
        {
            Id = application.Id,
            ClientId = application.ClientId,
            DisplayName = application.DisplayName,
            ClientType = application.ClientType,
            ConsentType = application.ConsentType,
            CreatedAt = application.CreatedAt,
            CreatedByUserId = application.CreatedByUserId,
            UpdatedAt = application.UpdatedAt,
            UpdatedByUserId = application.UpdatedByUserId,
            DeletedAt = application.DeletedAt,
            DeletedByUserId = application.DeletedByUserId,
            IsDeleted = application.IsDeleted,
            IsEnabled = application.IsEnabled
        }).ToList());
    }
}
