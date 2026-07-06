# Deployment

This document describes the basic deployment requirements for Auth2Demo.

## Requirements

- .NET 10 Runtime or SDK
- SQL Server
- HTTPS endpoint
- Valid connection string
- Proper redirect URI configuration for each client
- Secure secret storage for production settings

## Local development

Restore dependencies:

```bash
dotnet restore
```

Build the solution:

```bash
dotnet build
```

Apply migrations:

```bash
dotnet ef database update --project Auth2Demo.Infrastructure --startup-project Auth2Demo.Web
```

Run the web project:

```bash
dotnet run --project Auth2Demo.Web
```

## Entity Framework migrations

The Infrastructure project owns migrations. Use the Web project as the startup project because it contains the runtime configuration and dependency injection setup.

```bash
dotnet ef migrations add MigrationName --project Auth2Demo.Infrastructure --startup-project Auth2Demo.Web --output-dir Persistence/Migrations
```

## HTTPS

OAuth and OpenID Connect deployments should use HTTPS. Local development can use the ASP.NET Core development certificate, but production environments must use a trusted certificate.

## Reverse proxy considerations

When deploying behind IIS, Nginx, Cloudflare, a load balancer, or another reverse proxy, make sure forwarded headers are configured correctly so the application can resolve the public scheme and host.

Important headers:

- X-Forwarded-For
- X-Forwarded-Proto
- X-Forwarded-Host

Incorrect forwarded header configuration can cause invalid redirect URIs, wrong HTTP/HTTPS generation, and authentication callback issues.

## Production configuration

Recommended production practices:

- Store connection strings and secrets outside source control
- Use environment variables or a secure secret provider
- Enable HTTPS only
- Review cookie settings
- Validate redirect URIs and post logout redirect URIs
- Rotate client secrets regularly
- Keep audit data enabled
- Restrict admin access to authorized users only
- Use strong database backups

## Client deployment checklist

For each client application:

- Register the correct ClientId
- Configure allowed redirect URIs
- Configure allowed post logout redirect URIs
- Select the correct grant types
- Configure required scopes and API permissions
- Create or rotate client secrets when needed
- Configure branding if the client requires a custom experience
- Configure authentication methods for the client
- Test the complete authorization flow
