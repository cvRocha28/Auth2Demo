# Database

Auth2Demo uses Entity Framework Core with SQL Server. The database stores identity platform configuration, OpenIddict data, users, roles, branding settings, external identity providers, client secrets, and audit records.

## Main concepts

The database supports:

- OpenIddict applications
- OpenIddict authorizations
- OpenIddict scopes
- OpenIddict tokens
- ASP.NET Core Identity users and roles
- Identity providers
- Branding settings
- Client secret metadata
- Audit logs
- Application audit records
- Application secret audit records

## Client secrets

Client secrets are stored in a dedicated structure instead of being treated as a single static value on the application.

This enables:

- Multiple active secrets per client
- Secret rotation without downtime
- Secret expiration
- Secret revocation
- Secret prefix display for safe identification
- Secret lifecycle auditing

Revoked and expired secrets remain useful for audit and troubleshooting scenarios.

## Identity providers

External providers are stored in the IdentityProviders table. This allows the administration portal and authentication screen to dynamically load enabled providers.

Provider data is used by:

- Identity Providers administration screen
- Client Branding Authentication Methods tab
- Login/authentication UI
- Live preview

## Branding settings

Global branding is stored in the IdentityBrandingSettings table. Client-specific branding is stored as metadata associated with the OpenIddict application.

This design allows Auth2Demo to have a global default theme while also supporting per-client white-label customization.

## Auditing

Audit tables are used to retain important security and administration events.

Auditing should be used for:

- Client creation and updates
- Secret creation, expiration, and revocation
- Security-sensitive administration changes
- Identity provider changes
- Branding and authentication method changes

## Migrations

Migrations are stored in the Infrastructure project.

Create a migration:

```bash
dotnet ef migrations add MigrationName --project Auth2Demo.Infrastructure --startup-project Auth2Demo.Web --output-dir Persistence/Migrations
```

Apply migrations:

```bash
dotnet ef database update --project Auth2Demo.Infrastructure --startup-project Auth2Demo.Web
```

## Seeding

The infrastructure layer contains database seeding for initial platform data such as identity providers, branding defaults, scopes, users, and other required records.

Seed data should be kept safe, repeatable, and idempotent so the application can initialize development environments reliably.
