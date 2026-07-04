using Auth2Demo.Web.Areas.Admin.Models;
using Auth2Demo.Web.Security;
using Auth2Demo.Infrastructure.Services.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Auth2Demo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = AuthPolicies.Admin)]
public sealed class AuditLogsController : Controller
{
    private readonly IAdminAuditLogService _auditLogs;
    public AuditLogsController(IAdminAuditLogService auditLogs) => _auditLogs = auditLogs;

    public async Task<IActionResult> Index(string? q, string? category)
    {
        var model = new AuditLogsIndexViewModel
        {
            Query = q,
            Category = category,
            Categories = await _auditLogs.GetCategoriesAsync(),
            Logs = await _auditLogs.SearchAsync(q, category)
        };
        return View(model);
    }
}
