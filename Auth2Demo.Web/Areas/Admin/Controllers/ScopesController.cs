using Auth2Demo.Web.Areas.Admin.Models;
using Auth2Demo.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;

namespace Auth2Demo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = AuthPolicies.ClientManager)]
public sealed class ScopesController : Controller
{
    private readonly IOpenIddictScopeManager _scopeManager;

    public ScopesController(IOpenIddictScopeManager scopeManager)
    {
        _scopeManager = scopeManager;
    }

    public async Task<IActionResult> Index()
    {
        var scopes = new List<ScopeListItemViewModel>();

        await foreach (var scope in _scopeManager.ListAsync(100, 0))
        {
            scopes.Add(new ScopeListItemViewModel
            {
                Name = await _scopeManager.GetNameAsync(scope) ?? string.Empty,
                DisplayName = await _scopeManager.GetDisplayNameAsync(scope) ?? string.Empty,
                Description = await _scopeManager.GetDescriptionAsync(scope)
            });
        }

        return View(scopes.OrderBy(x => x.Name).ToArray());
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

        var existingScope = await _scopeManager.FindByNameAsync(model.Name);

        if (existingScope is null)
        {
            var descriptor = new OpenIddictScopeDescriptor
            {
                Name = model.Name,
                DisplayName = model.DisplayName,
                Description = model.Description
            };

            var resources = model.Resources?
                .Split(
                    new[] { ' ', ',', ';', '\r', '\n', '\t' },
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                ?? Array.Empty<string>();

            foreach (var resource in resources)
            {
                descriptor.Resources.Add(resource);
            }

            await _scopeManager.CreateAsync(descriptor);
        }

        return RedirectToAction(nameof(Index));
    }
}