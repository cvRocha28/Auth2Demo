# Auth2Demo

Auth2Demo is a modern Identity Provider built with ASP.NET Core 10 and OpenIddict. The project is designed as a professional OAuth 2.0 and OpenID Connect server with an administration portal, client management, branding customization, localization, auditing, and support for external identity providers.

The goal of the project is to provide a production-oriented identity platform inspired by solutions such as Auth0 and Microsoft Entra ID, while keeping the codebase clean, extensible, and easy to evolve.

## Main features

- OAuth 2.0 and OpenID Connect support with OpenIddict
- Authorization Code flow with PKCE
- Client Credentials flow
- Refresh token support
- Client management through the administration portal
- Redirect URI and post logout redirect URI management
- API permissions and scopes
- Required claims per client
- Multiple client secrets per application
- Client secret rotation without downtime
- Application and secret auditing
- External identity provider management
- Per-client branding and white-label customization
- Professional theme presets
- Live preview for branding changes
- Per-client authentication method configuration
- Username and password authentication control
- Dynamic external provider visibility based on enabled Identity Providers
- Multi-factor authentication screens
- User profile portal
- Admin dashboard
- Localization with resource files
- Clean Architecture separation

## Authentication customization

Each client can have its own authentication experience. The Branding page allows administrators to configure visual identity, theme, colors, appearance, advanced layout options, and authentication methods.

The Authentication Methods tab allows administrators to control which sign-in options are available for a specific client:

- Username and password
- External identity providers enabled in the IdentityProviders table

These options are reflected in the live preview and in the real authentication flow.

## Branding and themes

Auth2Demo includes a default professional theme and additional theme presets for different enterprise scenarios. Client branding can customize:

- Tenant name
- Logo URL
- Support email
- Primary and secondary colors
- Accent color
- Background color
- Surface color
- Text color
- Border radius
- Hero title and subtitle
- Footer text
- Theme preset
- Login page appearance

## Architecture

The solution follows Clean Architecture principles and is organized into four main projects:

- `Auth2Demo.Domain`: domain entities and shared domain concepts
- `Auth2Demo.Application`: contracts, DTOs, use cases, and service interfaces
- `Auth2Demo.Infrastructure`: EF Core, OpenIddict persistence, repositories, services, migrations, and seeding
- `Auth2Demo.Web`: MVC UI, admin portal, authentication pages, localization, and branding resolution

## Documentation

Additional documentation is available in the `docs` folder:

- `docs/architecture.md`
- `docs/authentication.md`
- `docs/administration.md`
- `docs/database.md`
- `docs/deployment.md`
- `docs/localization.md`
- `docs/professional-roadmap.md`

## Requirements

- .NET 10 SDK
- SQL Server
- Visual Studio 2022 or another compatible IDE
- Entity Framework Core CLI tools

## Running the project

Restore dependencies:

```bash
dotnet restore
```

Build the solution:

```bash
dotnet build
```

Apply database migrations:

```bash
dotnet ef database update --project Auth2Demo.Infrastructure --startup-project Auth2Demo.Web
```

Run the web application:

```bash
dotnet run --project Auth2Demo.Web
```

## Current update summary

This version includes a professional UI/UX overhaul, Auth2Demo default branding restoration, new theme presets, per-client authentication method configuration, improved live preview behavior, better authentication screens, admin portal refinements, localization improvements, and general consistency fixes across the project.
