using Auth2Demo.Infrastructure.Persistence;
using Auth2Demo.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;

namespace Auth2Demo.Infrastructure.Identity;

public static class IdentityInfrastructureExtensions
{
    public static IServiceCollection AddAuth2DemoIdentityServer(this IServiceCollection services)
    {
        services
            .AddIdentity<ApplicationUser, ApplicationRole>(options =>
            {
                options.User.RequireUniqueEmail = true;

                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;

                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);

                options.SignIn.RequireConfirmedEmail = true;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        services.AddSingleton<IPostConfigureOptions<GoogleOptions>, ExternalProviderOptionsSetup>();
        services.AddSingleton<IPostConfigureOptions<MicrosoftAccountOptions>, ExternalProviderOptionsSetup>();

        services.AddAuthentication()
            .AddGoogle("Google", _ => { })
            .AddMicrosoftAccount("Microsoft", _ => { });

        services.AddOpenIddict()
            .AddCore(options =>
            {
                options.UseEntityFrameworkCore()
                    .UseDbContext<ApplicationDbContext>()
                    .ReplaceDefaultEntities<Guid>();
            })
            .AddServer(options =>
            {
                options.SetAuthorizationEndpointUris("/connect/authorize")
                    .SetTokenEndpointUris("/connect/token")
                    .SetUserInfoEndpointUris("/connect/userinfo")
                    .SetEndSessionEndpointUris("/connect/logout")
                    .SetIntrospectionEndpointUris("/connect/introspect")
                    .SetRevocationEndpointUris("/connect/revoke");

                options.AllowAuthorizationCodeFlow()
                    .AllowRefreshTokenFlow()
                    .AllowClientCredentialsFlow();

                options.RequireProofKeyForCodeExchange();

                options.RegisterScopes(
                    OpenIddictConstants.Scopes.OpenId,
                    OpenIddictConstants.Scopes.Email,
                    OpenIddictConstants.Scopes.Profile,
                    OpenIddictConstants.Scopes.Roles,
                    OpenIddictConstants.Scopes.OfflineAccess,
                    "auth2demo.api",
                    "rareles.api");

                options.AddDevelopmentEncryptionCertificate()
                    .AddDevelopmentSigningCertificate();

                options.UseAspNetCore()
                    .EnableAuthorizationEndpointPassthrough()
                    .EnableEndSessionEndpointPassthrough()
                    .EnableTokenEndpointPassthrough()
                    .EnableUserInfoEndpointPassthrough()
                    .EnableStatusCodePagesIntegration();
            })
            .AddValidation(options =>
            {
                options.UseLocalServer();
                options.UseAspNetCore();
            });

        return services;
    }
}
