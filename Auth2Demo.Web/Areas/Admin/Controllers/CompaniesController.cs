using Auth2Demo.Application.Services.Admin;
using Auth2Demo.Web.Areas.Admin.Models;
using Auth2Demo.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Auth2Demo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = AuthPolicies.Admin)]
public sealed class CompaniesController : Controller
{
    private readonly IAdminCompanyService _companies;

    public CompaniesController(IAdminCompanyService companies)
    {
        _companies = companies;
    }

    public async Task<IActionResult> Index()
    {
        var companies = await _companies.ListAsync();
        return View(companies.Select(x => new CompanyListItemViewModel
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
        }).ToArray());
    }

    [HttpGet]
    public IActionResult Create() => View("Edit", new CompanyEditViewModel
    {
        IsEnabled = true,
        Culture = "pt-BR",
        Country = "BR",
        TimeZone = "E. South America Standard Time"
    });

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var company = await _companies.GetForEditAsync(id);
        if (company is null) return NotFound();
        return View(new CompanyEditViewModel
        {
            Id = company.Id,
            Name = company.Name,
            DisplayName = company.DisplayName,
            Description = company.Description,
            DomainHint = company.DomainHint,
            Country = company.Country,
            Culture = company.Culture,
            TimeZone = company.TimeZone,
            IsEnabled = company.IsEnabled,
            IsDefault = company.IsDefault
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(CompanyEditViewModel model)
    {
        if (!ModelState.IsValid) return View("Edit", model);

        var result = await _companies.SaveAsync(new CompanyEditData
        {
            Id = model.Id,
            Name = model.Name,
            DisplayName = model.DisplayName,
            Description = model.Description,
            DomainHint = model.DomainHint,
            Country = model.Country,
            Culture = model.Culture,
            TimeZone = model.TimeZone,
            IsEnabled = model.IsEnabled,
            IsDefault = model.IsDefault
        });

        if (result.NotFound) return NotFound();
        if (result.Duplicated)
        {
            ModelState.AddModelError(string.Empty, "Já existe uma empresa com esse nome interno.");
            return View("Edit", model);
        }

        TempData["Success"] = result.Created ? "Empresa criada com sucesso." : "Empresa atualizada com sucesso.";
        return RedirectToAction(nameof(Index));
    }
}
