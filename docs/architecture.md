# Architecture

Auth2Demo follows a Clean Architecture-oriented structure. Dependencies point inward: Web depends on Application and Infrastructure, Infrastructure depends on Application and Domain, Application depends on Domain, and Domain has no project dependency.

```text
                ┌──────────────────┐
                │   Auth2Demo.Web  │
                └────────┬─────────┘
                         │
          ┌──────────────┴──────────────┐
          │                             │
┌─────────▼──────────┐       ┌──────────▼──────────┐
│ Auth2Demo.Application│       │Auth2Demo.Infrastructure│
└─────────┬──────────┘       └──────────┬──────────┘
          │                             │
          └──────────────┬──────────────┘
                         │
                ┌────────▼────────┐
                │ Auth2Demo.Domain│
                └─────────────────┘
```

## Domain

`Auth2Demo.Domain` contains business concepts and persistence-agnostic entities.

Main identity concepts:

- `Company`
- `CompanyUser`
- `CompanyGroup`
- `CompanyGroupMember`
- `IdentityProvider`
- `ApplicationTenantAssignment`
- `ApplicationIdentityProvider`
- `EnterpriseApplicationRole`
- `EnterpriseApplicationAssignment`
- `IdentityApplicationSecret`

Main security concepts:

- `SecuritySettings`
- `AuditLog`
- `BrandingSettings`
- `EmailTemplate`
- `MfaMethod`
- `PasskeyCredential`
- `Permission`
- `RolePermission`
- `UserSession`
- `UserDevice`

Domain entities should own invariants that do not require infrastructure. They must not reference controllers, Razor views, EF Core, OpenIddict managers, or HTTP objects.

## Application

`Auth2Demo.Application` defines use-case boundaries and transport-neutral models.

Responsibilities:

- service interfaces consumed by Web;
- DTOs and application records;
- repository abstractions;
- common abstractions such as `IApplicationDbContext`, `ICurrentUser`, and `IDateTimeProvider`;
- tenant-governance and enterprise-application access contracts;
- shared authorization role names.

Application contracts should describe intent rather than persistence details. Controllers should call application services instead of querying `ApplicationDbContext` directly.

## Infrastructure

`Auth2Demo.Infrastructure` contains implementation details:

- `ApplicationDbContext` and EF Core mappings;
- SQL Server configuration;
- ASP.NET Core Identity stores;
- OpenIddict persistence and server configuration;
- repositories and application service implementations;
- password policy resolution and dynamic password validation;
- external identity provider resolution;
- client secret generation and provider-secret protection;
- persistent Data Protection keys;
- database initialization and seed data.

Delete behavior and relational constraints belong here. Services should use transactions when one operation modifies multiple aggregates or association tables.

## Web

`Auth2Demo.Web` is the presentation and HTTP layer.

Responsibilities:

- MVC controllers and Razor views;
- request validation and antiforgery protection;
- authentication and authorization policies;
- route composition;
- localization and culture selection;
- UI view models;
- branding resolution;
- HTTP-specific error handling.

The Web project should not contain core access rules or raw persistence logic. View models are allowed to be UI-specific and should not leak into Domain.

## Runtime request flow

A typical administrative request follows this path:

```text
Razor form
  -> Admin controller
  -> Application service contract
  -> Infrastructure service/repository
  -> ApplicationDbContext / OpenIddict manager
  -> SQL Server
```

A typical authorization request follows this path:

```text
Client /connect/authorize request
  -> AuthorizationController
  -> local or external authentication
  -> tenant/provider resolution
  -> enterprise access evaluator
  -> OpenIddict principal and destinations
  -> authorization code/token
```

## Composition root

`Auth2Demo.Web/Program.cs` is the composition root. It registers:

- Application services;
- Infrastructure services;
- Identity and OpenIddict;
- MVC and Razor Pages;
- localization;
- branding resolution;
- forwarded headers;
- application cookies;
- authorization policies.

The startup pipeline initializes the database through `IApplicationInitializer` before mapping normal application traffic.

## Cross-cutting conventions

- Use UTC or `DateTimeOffset` for persisted timestamps.
- Use cancellation tokens in I/O-bound services when practical.
- Keep secrets out of logs, audit payloads, URLs, and validation messages.
- Use transactions for membership removal, assignment cleanup, group deletion, and other multi-table operations.
- Use explicit SQL Server delete behavior to avoid cascade cycles.
- Keep initialization idempotent.
- Keep resource strings synchronized across all supported cultures.
