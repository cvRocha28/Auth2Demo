# Auth2Demo

A modern Identity Platform built with ASP.NET Core 10, OpenIddict and ASP.NET Core Identity.

Auth2Demo is an authentication and authorization server inspired by platforms such as Auth0, Microsoft Entra ID and Duende IdentityServer. The project aims to provide a complete identity solution that can be self-hosted and customized for different applications.

---

## Features

### Authentication

- ASP.NET Core Identity
- Cookie Authentication
- External Login Providers
  - Google
  - Microsoft
- Email Confirmation
- Password Reset
- Account Lockout
- Remember Me
- Password Policies

### Authorization

- OpenID Connect
- OAuth 2.1
- Authorization Code Flow + PKCE
- Refresh Tokens
- Client Credentials Flow
- Role Based Authorization

### Administration

- Dashboard
- User Management
- Role Management
- Permission Management
- Identity Providers
- Security Settings
- Email Templates
- Audit Logs
- Sessions
- Passkeys
- Multi-Factor Authentication (MFA)
- Branding

### Internationalization

- English (en-US)
- Portuguese (pt-BR)
- IStringLocalizer
- Resource (.resx) localization

---

# Architecture

The solution follows a Clean Architecture inspired structure.

```
Auth2Demo
│
├── Auth2Demo.Domain
├── Auth2Demo.Application
├── Auth2Demo.Infrastructure
├── Auth2Demo.Web
│
├── Auth2Demo.UnitTests
└── Auth2Demo.IntegrationTests
```

Solution folders are used inside Visual Studio without creating physical `src` or `tests` folders.

---

# Technologies

- .NET 10
- ASP.NET Core MVC
- ASP.NET Core Identity
- OpenIddict
- Entity Framework Core
- SQL Server
- Razor Views
- Bootstrap
- Localization (.resx)

---

# Requirements

- .NET SDK 10
- SQL Server 2022 (or LocalDB)
- Visual Studio 2022 (latest)

---

# Configuration

Configure your connection string inside:

```
appsettings.json
```

or

```
User Secrets
```

Example:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=Auth2Demo;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

---

# Restore

```powershell
dotnet restore .\Auth2Demo.slnx
```

---

# Build

```powershell
dotnet build .\Auth2Demo.slnx
```

---

# Run

```powershell
dotnet run --project .\Auth2Demo.Web
```

---

# Database

## Add Migration

```powershell
dotnet ef migrations add InitialIdentityServer `
  --project .\Auth2Demo.Infrastructure\Auth2Demo.Infrastructure.csproj `
  --startup-project .\Auth2Demo.Web\Auth2Demo.Web.csproj
```

## Update Database

```powershell
dotnet ef database update `
  --project .\Auth2Demo.Infrastructure\Auth2Demo.Infrastructure.csproj `
  --startup-project .\Auth2Demo.Web\Auth2Demo.Web.csproj
```

---

# Localization

The project supports multiple languages using `IStringLocalizer`.

Current languages:

- English
- Portuguese

---

# Authentication Flows

- Login
- Register
- Email Confirmation
- Password Reset
- External Login
- Logout
- Multi-Factor Authentication (MFA)
- Passkeys

---

# Project Structure

```
Auth2Demo.Domain
    Entities
    Enums
    Interfaces

Auth2Demo.Application
    Services
    DTOs
    Interfaces

Auth2Demo.Infrastructure
    Persistence
    Identity
    Repositories
    Services

Auth2Demo.Web
    Areas
    Controllers
    Models
    Views
    Resources
    Security
```

---

# Tests

Run all tests:

```powershell
dotnet test
```

---

# Roadmap

- OAuth/OIDC Discovery (`/.well-known/openid-configuration`)
- Consent Screen
- Applications Management
- OAuth Clients
- Scopes
- Custom Claims
- Authorization Screen (`/connect/authorize`)
- White Label
- Complete Audit
- Metrics Dashboard
- Complete WebAuthn / Passkeys support

---

# Contributing

Contributions are welcome.

Feel free to open Issues or Pull Requests.

---

# License

This project is currently distributed without a license.

A license may be added in the future.

---

# Inspiration

- Auth0
- Microsoft Entra ID
- Duende IdentityServer
- OpenIddict
- ASP.NET Core Identity
