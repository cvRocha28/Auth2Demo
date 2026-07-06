using Auth2Demo.Domain.Identity;
using Auth2Demo.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
using Microsoft.AspNetCore.Authentication.OAuth.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Auth2Demo.Infrastructure.Security;

public sealed class ExternalProviderOptionsSetup :
    IPostConfigureOptions<GoogleOptions>,
    IPostConfigureOptions<MicrosoftAccountOptions>
{
    private const string DisabledClientId = "disabled-provider-client-id";
    private const string DisabledClientSecret = "disabled-provider-client-secret";

    private readonly IServiceScopeFactory _scopeFactory;

    public ExternalProviderOptionsSetup(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public void PostConfigure(string? name, GoogleOptions options)
    {
        if (!string.Equals(name, "Google", StringComparison.OrdinalIgnoreCase)) return;

        var provider = GetConfiguredProvider("Google", IdentityProviderKind.Google);
        if (provider is null)
        {
            ConfigureAsDisabled(options);
            return;
        }

        options.ClientId = provider.ClientId!;
        options.ClientSecret = provider.ClientSecret!;
        options.CallbackPath = string.IsNullOrWhiteSpace(provider.CallbackPath) ? "/signin-google" : provider.CallbackPath;

        if (!options.Scope.Contains("profile"))
        {
            options.Scope.Add("profile");
        }

        options.ClaimActions.MapJsonKey("urn:google:picture", "picture");
        options.ClaimActions.MapJsonKey("picture", "picture");
    }

    public void PostConfigure(string? name, MicrosoftAccountOptions options)
    {
        if (!string.Equals(name, "Microsoft", StringComparison.OrdinalIgnoreCase)) return;

        var provider = GetConfiguredProvider("Microsoft", IdentityProviderKind.Microsoft);
        if (provider is null)
        {
            ConfigureAsDisabled(options);
            return;
        }

        options.ClientId = provider.ClientId!;
        options.ClientSecret = provider.ClientSecret!;
        options.CallbackPath = string.IsNullOrWhiteSpace(provider.CallbackPath) ? "/signin-microsoft" : provider.CallbackPath;

        options.ClaimActions.MapJsonKey("urn:microsoft:picture", "picture");
        options.ClaimActions.MapJsonKey("picture", "picture");
    }

    private IdentityProvider? GetConfiguredProvider(string scheme, IdentityProviderKind kind)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return db.IdentityProviders
            .AsNoTracking()
            .FirstOrDefault(x =>
                x.Scheme == scheme &&
                x.Kind == kind &&
                x.IsEnabled &&
                x.ClientId != null &&
                x.ClientId != "" &&
                x.ClientSecret != null &&
                x.ClientSecret != "");
    }

    private static void ConfigureAsDisabled(GoogleOptions options)
    {
        // The OAuth middleware validates ClientId/ClientSecret even when the button is not shown on the screen.
        // Therefore dummy values are used only to prevent the application from breaking when the provider
        // estiver desabilitado ou sem credenciais no banco. O AccountController bloqueia o Challenge.
        options.ClientId = DisabledClientId;
        options.ClientSecret = DisabledClientSecret;
    }

    private static void ConfigureAsDisabled(MicrosoftAccountOptions options)
    {
        // The OAuth middleware validates ClientId/ClientSecret even when the button is not shown on the screen.
        // Therefore dummy values are used only to prevent the application from breaking when the provider
        // estiver desabilitado ou sem credenciais no banco. O AccountController bloqueia o Challenge.
        options.ClientId = DisabledClientId;
        options.ClientSecret = DisabledClientSecret;
    }
}
