# Auth2Demo

Auth2Demo is a multi-tenant Identity and Access Management platform built with ASP.NET Core 10, ASP.NET Core Identity, Entity Framework Core, SQL Server, and OpenIddict.

The solution provides OAuth 2.0 and OpenID Connect capabilities together with an administration portal inspired by Microsoft Entra ID. It supports application registrations, enterprise applications, tenant-scoped identity providers, users, groups, application assignments, roles, branding, localization, security policies, auditing, sessions, devices, MFA administration, and passkey administration.

> This repository is an evolving reference implementation. Review the production-readiness checklist before using it in a production environment.

## Highlights

- OAuth 2.0 and OpenID Connect server powered by OpenIddict.
- Authorization Code flow with PKCE, Client Credentials, and refresh tokens.
- ASP.NET Core Identity for local users, roles, external logins, and password management.
- Multi-tenant company directory with users and groups.
- Tenant-specific Microsoft and Google identity providers.
- App registrations separated from enterprise application access configuration.
- Allowed tenants and allowed providers per application.
- Optional mandatory user or group assignment per tenant.
- Application-specific roles emitted as token claims.
- Dynamic security policy shared by registration, profile password creation, password change, and reset flows.
- Client secrets with hashing, prefix display, expiration, revocation, and audit visibility.
- Per-client branding and authentication method selection.
- Localization for `pt-BR` and `en-US` with user-profile culture resolution.
- Persistent ASP.NET Core Data Protection keys in SQL Server.
- Administrative dashboards, audit views, sessions, devices, MFA, passkeys, email templates, and health pages.
- Clean Architecture-oriented separation across Domain, Application, Infrastructure, and Web.

## Solution structure

```text
Auth2Demo.Domain
Auth2Demo.Application
Auth2Demo.Infrastructure
Auth2Demo.Web
Auth2Demo.UnitTests
Auth2Demo.IntegrationTests
```

| Project | Responsibility |
|---|---|
| `Auth2Demo.Domain` | Domain entities, enums, invariants, and security concepts. It does not depend on ASP.NET Core, EF Core, or the UI. |
| `Auth2Demo.Application` | Use-case contracts, DTOs, application models, abstractions, and authorization evaluation contracts. |
| `Auth2Demo.Infrastructure` | EF Core persistence, SQL Server mappings, repositories, application services, ASP.NET Core Identity, OpenIddict, Data Protection, provider secret protection, and database initialization. |
| `Auth2Demo.Web` | MVC controllers, Razor views, HTTP endpoints, authorization policies, localization, branding resolution, and the administration portal. |
| `Auth2Demo.UnitTests` | Fast domain and application-level tests. |
| `Auth2Demo.IntegrationTests` | End-to-end application and infrastructure tests using the Web host. |

## Core concepts

### Company / tenant

A company represents an internal Auth2Demo tenant, such as Atento or Interfile. Users may belong to more than one company through `CompanyUser` memberships. Groups are owned by one company.

### App registration

An app registration is the OAuth/OIDC application definition stored by OpenIddict. It contains the client identifier, client type, endpoints, grant types, redirect URIs, permissions, scopes, secrets, branding, and authentication options.

### Enterprise application

The enterprise application view controls how an app registration is consumed by tenants. It defines:

- the owner company;
- allowed companies;
- whether user assignment is mandatory for each company;
- allowed identity providers;
- application roles;
- direct user and group assignments.

### Identity provider

An identity provider belongs to a company. This allows separate Microsoft Entra ID or Google configurations for each tenant. An application can expose only the providers explicitly allowed for it.

### Assignment evaluation

When a tenant has `Require user assignment` enabled for an enterprise application, access is granted only when the authenticated user has a direct assignment or belongs to an assigned group. Application roles from those assignments can be included in the issued token.

## Requirements

- .NET 10 SDK.
- SQL Server 2019 or newer, SQL Server Developer, or a compatible Azure SQL environment.
- Entity Framework Core CLI tools.
- A trusted HTTPS certificate for production.
- Google and/or Microsoft application registrations when external authentication is enabled.

Install or update the EF CLI tool:

```powershell
dotnet tool update --global dotnet-ef
```

## Local configuration

Configure the database connection in `Auth2Demo.Web/appsettings.json`, user secrets, or environment variables:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=Auth2Demo;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

Do not commit production credentials or provider client secrets.

## Database creation

This package intentionally does not include a migrations folder. Create a fresh migration from the current model:

```powershell
dotnet clean
dotnet restore
dotnet build

dotnet ef migrations add InitialCreate `
  --project Auth2Demo.Infrastructure `
  --startup-project Auth2Demo.Web `
  --output-dir Persistence\Migrations

dotnet ef database update `
  --project Auth2Demo.Infrastructure `
  --startup-project Auth2Demo.Web
```

The Infrastructure project owns migrations. The Web project is the startup project because it supplies runtime configuration and dependency injection.

## Running locally

```powershell
dotnet run --project Auth2Demo.Web
```

The default Visual Studio profile uses HTTPS. Confirm the active URL in `Auth2Demo.Web/Properties/launchSettings.json`.

Main areas:

```text
/Account/Login
/Account/Register
/Perfil
/Admin/Dashboard
/Admin/Directory
/Admin/Companies
/Admin/Clients
/Admin/EnterpriseApplications
/Admin/SecuritySettings
```

## Default initialization

At startup, `IApplicationInitializer` runs the database initializer. Initialization code should remain idempotent because it may execute every time the application starts.

Review seed users, roles, clients, redirect URIs, provider configuration, and default credentials before deploying anywhere outside a local development environment.

## Security model

- Admin endpoints are protected by role-based policies.
- Local password requirements are loaded from persisted security settings.
- Client secrets are not stored in plaintext.
- External provider secrets are protected before persistence.
- Data Protection keys are persisted in the database so cookie and token-protection keys survive restarts and multi-instance deployment.
- HTTPS is required by the application cookie configuration.
- Forwarded headers are enabled for reverse-proxy deployments.

See [Security](docs/security.md) and [Deployment](docs/deployment.md) before production use.

## Documentation

Start with the [documentation index](docs/README.md).

Important documents:

- [Architecture](docs/architecture.md)
- [Identity and authentication](docs/authentication.md)
- [Tenant governance](docs/tenant-governance.md)
- [Directory administration](docs/tenant-directory.md)
- [Applications and enterprise applications](docs/applications.md)
- [Administration portal](docs/administration.md)
- [Database model and migrations](docs/database.md)
- [Configuration](docs/configuration.md)
- [Security](docs/security.md)
- [Deployment and operations](docs/deployment.md)
- [Localization](docs/localization.md)
- [Testing](docs/testing.md)
- [Professional roadmap](docs/professional-roadmap.md)

## Validation before pull requests

```powershell
dotnet clean
dotnet restore
dotnet build
dotnet test
```

For schema changes, also generate a temporary migration and inspect its SQL before committing it.

## License

No license file is currently included. Add an explicit license before distributing or using the project outside its intended environment.
