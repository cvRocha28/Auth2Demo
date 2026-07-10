using Auth2Demo.Application.Services.Admin;
using Auth2Demo.Web.Areas.Admin.Models;
using Auth2Demo.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Auth2Demo.Web;

namespace Auth2Demo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = AuthPolicies.Admin)]
public sealed class IdentityProvidersController : Controller
{
    private readonly IAdminIdentityProviderService _providers;
    private readonly IAdminCompanyService _companies;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public IdentityProvidersController(
        IAdminIdentityProviderService providers,
        IAdminCompanyService companies,
        IStringLocalizer<SharedResource> localizer)
    {
        _providers = providers;
        _companies = companies;
        _localizer = localizer;
    }

    public async Task<IActionResult> Index()
    {
        var providers = await _providers.ListAsync();
        return View(providers.Select(x => new IdentityProviderListItemViewModel
        {
            Id = x.Id,
            CompanyId = x.CompanyId,
            CompanyName = x.CompanyName,
            Name = x.Name,
            DisplayName = x.DisplayName,
            Scheme = x.Scheme,
            Kind = x.Kind,
            IsEnabled = x.IsEnabled,
            IsSystemProvider = x.IsSystemProvider,
            SortOrder = x.SortOrder,
            HasClientId = x.HasClientId,
            HasClientSecret = x.HasClientSecret
        }).ToArray());
    }

    [HttpGet]
    public async Task<IActionResult> Create() => View("Edit", await PrepareEditModelAsync(new IdentityProviderEditViewModel()));

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var model = await _providers.GetForEditAsync(id);
        return model is null ? NotFound() : View(await PrepareEditModelAsync(new IdentityProviderEditViewModel
        {
            Id = model.Id,
            CompanyId = model.CompanyId,
            Name = model.Name,
            DisplayName = model.DisplayName,
            Scheme = model.Scheme,
            Kind = model.Kind,
            IconCssClass = model.IconCssClass,
            ButtonText = model.ButtonText,
            ClientId = model.ClientId,
            ClientSecret = null,
            HasClientSecret = model.HasClientSecret,
            Authority = model.Authority,
            CallbackPath = model.CallbackPath,
            IsEnabled = model.IsEnabled,
            IsSystemProvider = model.IsSystemProvider,
            SortOrder = model.SortOrder
        }));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(IdentityProviderEditViewModel model)
    {
        if (!ModelState.IsValid) return View("Edit", await PrepareEditModelAsync(model));

        var result = await _providers.SaveAsync(new IdentityProviderEditData
        {
            Id = model.Id,
            CompanyId = model.CompanyId,
            Name = model.Name,
            DisplayName = model.DisplayName,
            Scheme = model.Scheme,
            Kind = model.Kind,
            IconCssClass = model.IconCssClass,
            ButtonText = model.ButtonText,
            ClientId = model.ClientId,
            ClientSecret = model.ClientSecret,
            HasClientSecret = model.HasClientSecret,
            Authority = model.Authority,
            CallbackPath = model.CallbackPath,
            IsEnabled = model.IsEnabled,
            IsSystemProvider = model.IsSystemProvider,
            SortOrder = model.SortOrder
        });
        if (result.NotFound) return NotFound();
        if (result.Duplicated)
        {
            ModelState.AddModelError(string.Empty, _localizer["IdentityProviderAlreadyExists"].Value);
            return View("Edit", await PrepareEditModelAsync(model));
        }

        TempData["Success"] = result.Created ? _localizer["ProviderCreatedSuccessfully"].Value : _localizer["ProviderUpdatedSuccessfully"].Value;
        return RedirectToAction(nameof(Index));
    }

    private async Task<IdentityProviderEditViewModel> PrepareEditModelAsync(IdentityProviderEditViewModel model)
    {
        var companies = await _companies.ListEnabledAsync();
        model.Companies = companies.Select(x => new CompanyListItemViewModel
        {
            Id = x.Id,
            Name = x.Name,
            DisplayName = x.DisplayName,
            DomainHint = x.DomainHint,
            Country = x.Country,
            Culture = x.Culture,
            IsEnabled = x.IsEnabled,
            IsDefault = x.IsDefault,
            ProviderCount = x.ProviderCount
        }).ToArray();
        return model;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(Guid id)
    {
        var result = await _providers.ToggleAsync(id);
        if (result.NotFound) return NotFound();
        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _providers.DeleteAsync(id);
        if (result.NotFound) return NotFound();
        if (result.IsSystemProvider)
        {
            TempData["Error"] = _localizer["SystemProvidersCannotBeDeleted"].Value;
            return RedirectToAction(nameof(Index));
        }
        TempData["Success"] = _localizer["ProviderDeletedSuccessfully"].Value;
        return RedirectToAction(nameof(Index));
    }
}
