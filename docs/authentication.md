# Identity and authentication

Auth2Demo combines ASP.NET Core Identity for user management with OpenIddict for OAuth 2.0 and OpenID Connect protocol endpoints.

## Local authentication

Local users authenticate with the ASP.NET Core Identity account store. The account flows include:

- registration;
- sign-in and sign-out;
- local password creation for externally created users;
- password change;
- password reset infrastructure;
- lockout and password validation;
- profile localization preferences.

The active password policy is loaded from `SecuritySettings`. The same effective policy must be enforced by server-side Identity validation in every password-writing flow. Client-side checklists are informational and must not replace server validation.

## External authentication

Google and Microsoft authentication handlers are registered. Provider configuration is database-driven through tenant-owned `IdentityProvider` records.

At external sign-in, the application can persist available profile information such as:

- provider subject identifier;
- e-mail and verification state;
- display name;
- language or locale;
- country when supplied;
- culture and time-zone preferences when resolved.

Provider claims vary. Country and time zone must not be assumed to exist. Browser culture and explicit profile selection remain the reliable fallback.

## Tenant-specific providers

Each identity provider belongs to a company. This supports configurations such as:

```text
Atento -> Microsoft Entra provider for the Atento tenant
Interfile -> Microsoft Entra provider for the Interfile tenant
```

An enterprise application may allow providers from multiple companies. The provider selected during sign-in must be validated against the providers enabled for the target application.

## OAuth 2.0 and OpenID Connect

OpenIddict provides the protocol server and EF Core persistence.

Supported project scenarios include:

- Authorization Code with PKCE;
- Client Credentials;
- refresh tokens;
- registered redirect and post-logout redirect URIs;
- scopes and permissions;
- client secrets;
- authorization and token persistence.

Public clients should use Authorization Code with PKCE and must not rely on a client secret. Confidential clients must protect their credentials.

## Authorization flow

A simplified interactive flow is:

```text
1. Client redirects the browser to /connect/authorize.
2. Auth2Demo validates the OpenIddict request and client.
3. The application resolves branding and enabled authentication methods.
4. The user authenticates locally or through an allowed external provider.
5. Tenant membership and enterprise application access are evaluated.
6. Application roles and approved claims are added to the principal.
7. OpenIddict issues the authorization response.
```

## Enterprise access evaluation

The evaluator considers:

- the requested application;
- the authenticated user's active company memberships;
- allowed tenant configuration;
- whether assignment is required;
- direct user assignments;
- group assignments;
- enabled application roles.

When assignment is required and no valid assignment exists, authentication may be valid while application access is denied. The UI should show a clear access-not-assigned message and a correlation identifier without exposing sensitive internals.

## Cookies

The application cookie is configured with:

- name `Auth2Demo.Identity`;
- `HttpOnly` enabled;
- secure transport required;
- `SameSite=Lax`;
- sliding expiration;
- explicit login, logout, and access-denied paths.

Review SameSite behavior whenever external providers, cross-site frontends, or reverse proxies are introduced.

## Data Protection

ASP.NET Core Data Protection keys are stored in SQL Server under `IdentityDataProtectionKeys` and use the application name `Auth2Demo.IdentityServer`. All instances in the same deployment that must share cookies should use the same key ring and application name.

## Profile culture

Culture resolution order is:

1. authenticated user's profile preference;
2. culture cookie;
3. query string;
4. `Accept-Language` header;
5. default `en-US`.

Supported cultures are currently `pt-BR` and `en-US`.
