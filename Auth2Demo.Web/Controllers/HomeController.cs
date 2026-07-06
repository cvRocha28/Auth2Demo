using Auth2Demo.Application.Services.Portal;
using Auth2Demo.Infrastructure.Identity;
using Auth2Demo.Web.Models;
using Auth2Demo.Web.Models.Account;
using Auth2Demo.Web.Models.Home;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace Auth2Demo.Web.Controllers;

public sealed class HomeController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IExternalProviderService _externalProviders;

    public HomeController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IExternalProviderService externalProviders)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _externalProviders = externalProviders;
    }

    public async Task<IActionResult> Index(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null)
            {
                await _signInManager.SignOutAsync();
                return RedirectToAction("Login", "Account");
            }

            return RedirectToAction("Index", "Perfil");
        }

        var providers = (await _externalProviders.GetEnabledForLoginAsync())
            .Select(x => new ExternalProviderViewModel
            {
                DisplayName = x.DisplayName,
                Scheme = x.Scheme,
                ButtonText = x.ButtonText
            })
            .ToArray();

        return View(new HomeIndexViewModel
        {
            Login = new LoginViewModel { ReturnUrl = returnUrl, ExternalProviders = providers },
            ExternalProviders = providers
        });
    }

    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
