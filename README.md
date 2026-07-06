
# Auth2Demo

Auth2Demo is a modern Identity Provider built with ASP.NET Core 10 and OpenIddict, inspired by Azure Entra ID and Auth0.

## Features

- OAuth 2.1 / OpenID Connect
- Authorization Code + PKCE
- Client Credentials
- Multiple Client Secrets (DEV/QA/UAT/PRD)
- Secret rotation without revoking active secrets
- API Permissions
- Redirect URI management
- Required Claims
- Identity Providers
- Audit logging
- Localization (i18n)
- Clean Architecture

## Architecture

- Auth2Demo.Domain
- Auth2Demo.Application
- Auth2Demo.Infrastructure
- Auth2Demo.Web

See the documentation inside the `docs` folder.

## Running

```bash
dotnet restore
dotnet build
dotnet ef database update
dotnet run --project Auth2Demo.Web
```

## Roadmap

See docs/professional-roadmap.md
