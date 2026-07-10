# Applications and enterprise applications

Auth2Demo separates the protocol definition of an application from its tenant-specific consumption policy.

## App registrations

The app registration area is available under `/Admin/Clients` and maps to OpenIddict applications plus Auth2Demo extension tables.

Configuration includes:

- client ID and display name;
- public or confidential client type;
- consent type;
- allowed grant types and endpoints;
- redirect URIs and post-logout redirect URIs;
- scopes and permissions;
- required claims;
- client secrets;
- branding;
- allowed authentication methods.

## Enterprise applications

The enterprise applications area is available under `/Admin/EnterpriseApplications`.

It controls:

- owner company;
- allowed companies;
- `Require user assignment` per company;
- allowed external identity providers;
- user and group assignments;
- application roles.

This separation mirrors the distinction between an application object and its tenant-specific service-principal behavior.

## Client secrets

`IdentityApplicationSecret` stores secret metadata and a hash rather than the original secret.

Recommended lifecycle:

1. Generate a cryptographically random secret.
2. Display the plaintext value once.
3. Persist only the hash and a short display prefix.
4. Set an explicit expiration date.
5. Create a replacement before revoking the old secret.
6. Audit creation, revocation, and expiration.

Never place client secrets in source control, logs, URLs, screenshots, or support tickets.

## Scopes and permissions

Scopes define delegated access requested by client applications. OpenIddict permissions define what a client is allowed to request and which endpoints or grant types it may use.

Scope descriptions shown to users should be understandable and suitable for a consent screen. Internal implementation names should not be exposed without explanation.

## Branding and authentication methods

Each app registration can customize the authentication experience. Branding includes colors, logo, text, layout, and theme preferences.

Authentication methods can restrict the login page to:

- username and password;
- selected tenant-owned external providers.

The visible login options and the server-side accepted providers must use the same configuration source.

## Application roles

Application roles are defined on the enterprise application and assigned to users or groups. Enabled roles obtained from valid assignments may be emitted in the `roles` claim.

A role value should be stable, machine-readable, and independent of its localized display name.

## Validation rules

- Redirect URIs must be exact and use HTTPS outside local development.
- Public clients must not require a secret.
- Confidential clients must use protected credentials.
- Providers must belong to an allowed tenant.
- Assignments must use users or groups from the selected tenant.
- An assigned application role must belong to the same application.
- Disabled tenants, memberships, groups, providers, assignments, or roles must not grant access.
