# Auth2Demo

A modern Identity Platform built with ASP.NET Core 10, OpenIddict and ASP.NET Core Identity.

## Features

- ASP.NET Core Identity
- OpenIddict
- OAuth 2.1 / OpenID Connect
- External Login Providers
- MFA
- Passkeys
- Email Confirmation
- Password Reset
- Administration Portal
- Localization (en-US / pt-BR)

## Architecture

The solution follows a Clean Architecture inspired approach.

```
Auth2Demo
├── Auth2Demo.Domain
├── Auth2Demo.Application
├── Auth2Demo.Infrastructure
├── Auth2Demo.Web
├── Auth2Demo.UnitTests
└── Auth2Demo.IntegrationTests
```

## Restore

```powershell
dotnet restore .\Auth2Demo.slnx
```

## Build

```powershell
dotnet build .\Auth2Demo.slnx
```

## Run

```powershell
dotnet run --project .\Auth2Demo.Web
```

## Documentation

- [Architecture](docs/architecture.md)
- [Authentication](docs/authentication.md)
- [Localization](docs/localization.md)
- [Deployment](docs/deployment.md)
- [Professional Roadmap](docs/professional-roadmap.md)
