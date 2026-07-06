using Auth2Demo.Application.Services.Admin;
using Auth2Demo.Web.Areas.Admin.Models;
using Auth2Demo.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Auth2Demo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = AuthPolicies.ClientManager)]
public sealed class ScopesController : Controller
{
    private readonly IScopeAdminService _scopes;

    public ScopesController(IScopeAdminService scopes)
    {
        _scopes = scopes;
    }

    public async Task<IActionResult> Index()
    {
        var scopes = await _scopes.ListAsync();

        return View(scopes.Select(scope => new ScopeListItemViewModel
        {
            Name = scope.Name,
            DisplayName = scope.DisplayName,
            Description = scope.Description
        }).ToArray());
    }

    public IActionResult Create()
    {
        return View(new ScopeCreateViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ScopeCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var resources = (model.Resources ?? string.Empty)
            .Split([' ', ',', ';', '\r', '\n', '\t'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        await _scopes.CreateIfMissingAsync(new ScopeCreateData(
            model.Name,
            model.DisplayName,
            model.Description,
            resources));

        return RedirectToAction(nameof(Index));
    }
}
