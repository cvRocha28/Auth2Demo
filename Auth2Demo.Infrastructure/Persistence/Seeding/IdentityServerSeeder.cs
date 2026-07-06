using Auth2Demo.Domain.Identity;
using Auth2Demo.Domain.Security;
using Auth2Demo.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Auth2Demo.Application.Security;
using Microsoft.AspNetCore.Identity;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Auth2Demo.Infrastructure.Persistence.Seeding;

public static class IdentityServerSeeder
{
    public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration)
    {
        await SeedRolesAsync(services.GetRequiredService<RoleManager<ApplicationRole>>());
        await SeedAdminAsync(services.GetRequiredService<UserManager<ApplicationUser>>());
        await SeedScopesAsync(services.GetRequiredService<IOpenIddictScopeManager>());
        await SeedClientsAsync(services.GetRequiredService<IOpenIddictApplicationManager>());
        var db = services.GetRequiredService<ApplicationDbContext>();
        await SeedIdentityProvidersAsync(db);
        await SeedPortalDefaultsAsync(db);
    }

    private static async Task SeedRolesAsync(RoleManager<ApplicationRole> roleManager)
    {
        var roles = new[]
        {
            (Name: AuthRoles.Admin, Description: "Administrador total do Identity Provider"),
            (Name: AuthRoles.ClientManager, Description: "Manages applications, clients, secrets, and APIs"),
            (Name: AuthRoles.UserManager, Description: "Manages users, lockouts, and roles")
        };

        foreach (var role in roles)
        {
            if (await roleManager.RoleExistsAsync(role.Name))
            {
                continue;
            }

            var result = await roleManager.CreateAsync(new ApplicationRole
            {
                Id = Guid.NewGuid(),
                Name = role.Name,
                Description = role.Description,
                IsSystemRole = true
            });

            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    string.Join("; ", result.Errors.Select(error => error.Description)));
            }
        }
    }

    private static async Task SeedAdminAsync(UserManager<ApplicationUser> userManager)
    {
        const string email = "admin@auth2demo.local";
        const string password = "Admin@12345!";

        var admin = await userManager.FindByEmailAsync(email);

        if (admin is null)
        {
            admin = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                DisplayName = "Auth2Demo Admin",
                Status = UserStatus.Active,
                DocumentVerificationStatus = VerificationStatus.Approved,
                FaceVerificationStatus = VerificationStatus.Approved
            };

            var result = await userManager.CreateAsync(admin, password);

            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    string.Join("; ", result.Errors.Select(error => error.Description)));
            }
        }

        var currentRoles = await userManager.GetRolesAsync(admin);
        var rolesToAdd = new[]
        {
            AuthRoles.Admin,
            AuthRoles.ClientManager,
            AuthRoles.UserManager
        }.Except(currentRoles, StringComparer.OrdinalIgnoreCase);

        var addRolesResult = await userManager.AddToRolesAsync(admin, rolesToAdd);

        if (!addRolesResult.Succeeded)
        {
            throw new InvalidOperationException(
                string.Join("; ", addRolesResult.Errors.Select(error => error.Description)));
        }
    }


    private static async Task SeedIdentityProvidersAsync(ApplicationDbContext db)
    {
        async Task EnsureProviderAsync(
            string name,
            string displayName,
            string scheme,
            IdentityProviderKind kind,
            string buttonText,
            string callbackPath,
            int sortOrder)
        {
            var provider = await db.IdentityProviders.FirstOrDefaultAsync(x => x.Name == name);
            if (provider is not null)
            {
                return;
            }

            provider = new IdentityProvider(name, displayName, scheme, kind);
            provider.MarkAsSystemProvider();
            provider.Update(
                displayName,
                scheme,
                kind,
                iconCssClass: null,
                buttonText: buttonText,
                clientId: null,
                clientSecret: null,
                authority: null,
                callbackPath: callbackPath,
                isEnabled: false,
                sortOrder: sortOrder);

            db.IdentityProviders.Add(provider);
        }

        await EnsureProviderAsync("Google", "Google", "Google", IdentityProviderKind.Google, "Continuar com Google", "/signin-google", 10);
        await EnsureProviderAsync("Microsoft", "Microsoft", "Microsoft", IdentityProviderKind.Microsoft, "Continuar com Microsoft", "/signin-microsoft", 20);
        await EnsureProviderAsync("GitHub", "GitHub", "GitHub", IdentityProviderKind.GitHub, "Continuar com GitHub", "/signin-github", 30);

        await db.SaveChangesAsync();
    }



    private static async Task SeedPortalDefaultsAsync(ApplicationDbContext db)
    {
        if (!await db.SecuritySettings.AnyAsync())
        {
            db.SecuritySettings.Add(new SecuritySettings { Id = Guid.NewGuid() });
        }

        if (!await db.BrandingSettings.AnyAsync())
        {
            db.BrandingSettings.Add(new BrandingSettings
            {
                Id = Guid.NewGuid(),
                TenantName = "Auth2Demo",
                PrimaryColor = "#2563eb",
                SecondaryColor = "#0f172a",
                Theme = "Light"
            });
        }

        async Task EnsureTemplateAsync(string key, string name, string subject, string body)
        {
            if (await db.EmailTemplates.AnyAsync(x => x.Key == key)) return;
            db.EmailTemplates.Add(new EmailTemplate
            {
                Id = Guid.NewGuid(),
                Key = key,
                Name = name,
                Subject = subject,
                BodyHtml = body,
                IsEnabled = true
            });
        }

        await EnsureTemplateAsync("email-confirmation", "Email confirmation", "Confirm your email", "<h1>Confirm your email</h1><p>Use the link below to confirm your account.</p>");
        await EnsureTemplateAsync("password-reset", "Password recovery", "Reset your password", "<h1>Password reset</h1><p>Use the link below to create a new password.</p>");
        await EnsureTemplateAsync("mfa-code", "MFA code", "Your verification code", "<h1>Verification code</h1><p>Your code is: {{code}}</p>");
        await EnsureTemplateAsync("welcome", "Welcome", "Welcome to Auth2Demo", "<h1>Welcome</h1><p>Your account was created successfully.</p>");
        await EnsureTemplateAsync("account-blocked", "Account blocked", "Your account has been blocked", "<h1>Account blocked</h1><p>Entre em contato com o suporte.</p>");

        var permissions = new[]
        {
            ("users.read", "Read users", "Users"),
            ("users.write", "Manage users", "Users"),
            ("roles.write", "Gerenciar roles", "Authorization"),
            ("permissions.write", "Manage permissions", "Authorization"),
            ("providers.write", "Gerenciar providers", "Authentication"),
            ("clients.write", "Gerenciar clients", "Applications"),
            ("apis.write", "Gerenciar APIs e scopes", "Applications"),
            ("audit.read", "Ler audit logs", "Security"),
            ("sessions.revoke", "Revoke sessions", "Security"),
            ("branding.write", "Edit branding", "Customization")
        };

        foreach (var p in permissions)
        {
            if (await db.Permissions.AnyAsync(x => x.Name == p.Item1)) continue;
            db.Permissions.Add(new Permission
            {
                Id = Guid.NewGuid(),
                Name = p.Item1,
                DisplayName = p.Item2,
                Category = p.Item3
            });
        }

        await db.SaveChangesAsync();
    }

    private static async Task SeedScopesAsync(IOpenIddictScopeManager scopeManager)
    {
        async Task EnsureScopeAsync(
            string name,
            string displayName,
            string resource,
            string? description = null)
        {
            if (await scopeManager.FindByNameAsync(name) is not null)
            {
                return;
            }

            var descriptor = new OpenIddictScopeDescriptor
            {
                Name = name,
                DisplayName = displayName,
                Description = description
            };

            descriptor.Resources.Add(resource);

            await scopeManager.CreateAsync(descriptor);
        }

        await EnsureScopeAsync(
            "auth2demo.api",
            "Auth2Demo API",
            "auth2demo_api",
            "API administrativa do provedor de identidade.");

        await EnsureScopeAsync(
            "rareles.api",
            "Rareles API",
            "rareles_api",
            "API principal do Rareles.");
    }

    private static async Task SeedClientsAsync(IOpenIddictApplicationManager manager)
    {
        if (await manager.FindByClientIdAsync("auth2demo-web") is null)
        {
            var descriptor = new OpenIddictApplicationDescriptor
            {
                ClientId = "auth2demo-web",
                ClientSecret = "auth2demo-web-secret",
                ConsentType = ConsentTypes.Explicit,
                DisplayName = "Auth2Demo Web MVC Client",
                ClientType = ClientTypes.Confidential
            };

            descriptor.RedirectUris.Add(new Uri("https://localhost:5002/signin-oidc"));
            descriptor.PostLogoutRedirectUris.Add(new Uri("https://localhost:5002/signout-callback-oidc"));

            descriptor.Permissions.Add(Permissions.Endpoints.Authorization);
            descriptor.Permissions.Add(Permissions.Endpoints.Token);
            descriptor.Permissions.Add(Permissions.Endpoints.EndSession);
            descriptor.Permissions.Add(Permissions.Endpoints.Revocation);

            descriptor.Permissions.Add(Permissions.GrantTypes.AuthorizationCode);
            descriptor.Permissions.Add(Permissions.GrantTypes.RefreshToken);

            descriptor.Permissions.Add(Permissions.ResponseTypes.Code);

            descriptor.Permissions.Add(Permissions.Prefixes.Scope + Scopes.OpenId);
            descriptor.Permissions.Add(Permissions.Prefixes.Scope + Scopes.Email);
            descriptor.Permissions.Add(Permissions.Prefixes.Scope + Scopes.Profile);
            descriptor.Permissions.Add(Permissions.Prefixes.Scope + Scopes.Roles);
            descriptor.Permissions.Add(Permissions.Prefixes.Scope + Scopes.OfflineAccess);
            descriptor.Permissions.Add(Permissions.Prefixes.Scope + "auth2demo.api");

            await manager.CreateAsync(descriptor);
        }

        if (await manager.FindByClientIdAsync("rareles-web") is null)
        {
            var descriptor = new OpenIddictApplicationDescriptor
            {
                ClientId = "rareles-web",
                DisplayName = "Rareles Web MVC Client",
                ConsentType = ConsentTypes.Explicit,
                ClientType = ClientTypes.Public
            };

            descriptor.RedirectUris.Add(new Uri("https://localhost:7108/signin-oidc"));
            descriptor.PostLogoutRedirectUris.Add(new Uri("https://localhost:7108/signout-callback-oidc"));

            descriptor.Permissions.Add(Permissions.Endpoints.Authorization);
            descriptor.Permissions.Add(Permissions.Endpoints.Token);
            descriptor.Permissions.Add(Permissions.Endpoints.EndSession);

            descriptor.Permissions.Add(Permissions.GrantTypes.AuthorizationCode);
            descriptor.Permissions.Add(Permissions.GrantTypes.RefreshToken);

            descriptor.Permissions.Add(Permissions.ResponseTypes.Code);

            descriptor.Permissions.Add(Permissions.Prefixes.Scope + Scopes.OpenId);
            descriptor.Permissions.Add(Permissions.Prefixes.Scope + Scopes.Email);
            descriptor.Permissions.Add(Permissions.Prefixes.Scope + Scopes.Profile);
            descriptor.Permissions.Add(Permissions.Prefixes.Scope + Scopes.Roles);
            descriptor.Permissions.Add(Permissions.Prefixes.Scope + Scopes.OfflineAccess);
            descriptor.Permissions.Add(Permissions.Prefixes.Scope + "rareles.api");

            descriptor.Requirements.Add(Requirements.Features.ProofKeyForCodeExchange);

            await manager.CreateAsync(descriptor);
        }

        if (await manager.FindByClientIdAsync("rareles-worker") is null)
        {
            var descriptor = new OpenIddictApplicationDescriptor
            {
                ClientId = "rareles-worker",
                ClientSecret = "rareles-worker-secret",
                DisplayName = "Rareles Worker M2M",
                ClientType = ClientTypes.Confidential
            };

            descriptor.Permissions.Add(Permissions.Endpoints.Token);
            descriptor.Permissions.Add(Permissions.GrantTypes.ClientCredentials);
            descriptor.Permissions.Add(Permissions.Prefixes.Scope + "rareles.api");

            await manager.CreateAsync(descriptor);
        }
    }
}
