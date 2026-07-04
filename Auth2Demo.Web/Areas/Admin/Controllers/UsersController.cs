using Auth2Demo.Web.Areas.Admin.Models;
using Auth2Demo.Web.Security;
using Auth2Demo.Infrastructure.Services.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Auth2Demo.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = AuthPolicies.UserManager)]
public sealed class UsersController : Controller
{
    private readonly IAdminUserService _users;
    public UsersController(IAdminUserService users) => _users = users;

    public async Task<IActionResult> Index(string? q)
    {
        return View(new UsersIndexViewModel
        {
            Query = q,
            Users = await _users.SearchAsync(q)
        });
    }

    public async Task<IActionResult> Create()
    {
        return View(new UserCreateViewModel
        {
            AvailableRoles = await _users.GetRoleNamesAsync()
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserCreateViewModel model)
    {
        model.AvailableRoles = await _users.GetRoleNamesAsync();
        if (!ModelState.IsValid) return View(model);

        var result = await _users.CreateAsync(new AdminUserCreateData { DisplayName = model.DisplayName, Email = model.Email, Password = model.Password, EmailConfirmed = model.EmailConfirmed, Roles = model.Roles });
        if (result.Success) return RedirectToAction(nameof(Index));

        foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
        return View(model);
    }

    public async Task<IActionResult> Edit(Guid id)
    {
        var model = await _users.GetForEditAsync(id);
        if (model is null) return NotFound();
        return View(new UserEditViewModel { Id = model.Id, DisplayName = model.DisplayName, Email = model.Email, Status = model.Status, EmailConfirmed = model.EmailConfirmed, Roles = model.Roles, AvailableRoles = await _users.GetRoleNamesAsync() });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UserEditViewModel model)
    {
        model.AvailableRoles = await _users.GetRoleNamesAsync();
        if (!ModelState.IsValid) return View(model);

        var result = await _users.UpdateAsync(new AdminUserEditData { Id = model.Id, DisplayName = model.DisplayName, Email = model.Email, Status = model.Status, EmailConfirmed = model.EmailConfirmed, Roles = model.Roles });
        if (result.NotFound) return NotFound();
        if (result.Success) return RedirectToAction(nameof(Index));

        foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleBlock(Guid id)
    {
        if (!await _users.ToggleBlockAsync(id)) return NotFound();
        return RedirectToAction(nameof(Index));
    }
}
