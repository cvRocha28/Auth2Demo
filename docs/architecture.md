# Architecture

Auth2Demo follows a Clean Architecture approach. The solution separates business rules, application contracts, persistence, infrastructure, and presentation concerns so the identity platform can evolve without tightly coupling the UI to database or provider-specific implementation details.

## Solution structure

```text
Auth2Demo.Domain
Auth2Demo.Application
Auth2Demo.Infrastructure
Auth2Demo.Web
```

## Auth2Demo.Domain

The Domain project contains the core entities and domain concepts used by the identity platform. It includes security-related entities such as branding settings, identity providers, audit records, and application secret metadata.

This layer should remain independent from ASP.NET Core, Entity Framework Core, OpenIddict implementation details, and UI concerns.

## Auth2Demo.Application

The Application project defines service contracts, DTOs, common abstractions, and application-level models. It acts as the boundary between the web layer and infrastructure implementation.

Main responsibilities:

- Define service contracts used by controllers
- Expose DTOs for clients, scopes, users, and administration data
- Define common abstractions such as current user and date/time provider
- Keep application logic independent from MVC and EF Core implementation details

## Auth2Demo.Infrastructure

The Infrastructure project implements persistence, repositories, application services, OpenIddict integration, migrations, and database seeding.

Main responsibilities:

- Entity Framework Core DbContext
- OpenIddict application persistence
- IdentityProvider persistence
- BrandingSettings persistence
- Client secret storage and rotation support
- Audit data storage
- Repository implementations
- Initial database seeding

## Auth2Demo.Web

The Web project contains the MVC application, authentication endpoints, admin portal, UI views, static assets, localization resources, and branding resolution.

Main responsibilities:

- Public authentication screens
- Authorization and consent flow
- Account management
- User profile portal
- Administration portal
- Client branding UI
- Authentication method configuration UI
- Localization and culture selection
- Runtime branding resolution

## Branding architecture

Branding is resolved at runtime by combining global branding settings with client-specific branding metadata. Client-specific branding is stored in the OpenIddict application properties under the Auth2Demo branding payload.

The branding resolver is responsible for:

- Reading global branding settings
- Detecting the current client from the OpenID Connect request
- Loading client-specific branding metadata
- Merging global and client-level settings
- Providing a view model used by authentication pages

## Authentication method architecture

Authentication method configuration is client-specific. The client branding configuration stores which methods are allowed for the client, including username/password and external identity providers.

External providers are loaded from enabled records in the IdentityProviders table. This allows the admin portal, live preview, and real login screen to stay consistent with the current provider configuration.

## Administration architecture

The administration portal is implemented under the Admin area. Controllers depend on application service contracts instead of accessing the database directly. This keeps the UI thin and allows business and persistence rules to remain testable and reusable.

## Design principles

- Keep domain and application layers independent from UI concerns
- Keep controllers focused on request handling and orchestration
- Use services and repositories for business and data access logic
- Store audit-relevant data instead of deleting important security history
- Make client-level authentication and branding configuration explicit
- Keep localization centralized through resources
