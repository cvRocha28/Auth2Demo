# Authentication

Auth2Demo provides OAuth 2.0 and OpenID Connect capabilities through OpenIddict. The project supports browser-based authentication, machine-to-machine flows, client-specific branding, external identity providers, and configurable authentication methods per client.

## Supported flows

- Authorization Code with PKCE
- Client Credentials
- Refresh Tokens
- External provider sign-in
- Local username and password sign-in

## Authorization Code with PKCE

The Authorization Code flow with PKCE is the recommended flow for browser-based and public clients. It avoids exposing secrets in public clients and protects the authorization code exchange with a code verifier and challenge.

## Client Credentials

The Client Credentials flow is supported for confidential clients and service-to-service communication. Confidential clients must have a valid client secret or another valid signing credential according to the OpenIddict configuration.

## Client secrets

Client secrets are stored separately from the OpenIddict application record using application secret metadata. This allows multiple active secrets per client and enables safe secret rotation.

Secret management supports:

- Multiple active secrets
- Secret prefixes for identification
- Expiration dates
- Revocation dates
- Auditing of secret lifecycle operations

## External identity providers

External providers are managed through the IdentityProviders administration screen. Enabled providers can be displayed on the authentication screen and can also be controlled per client through the Authentication Methods configuration.

Identity provider records include provider metadata such as name, scheme, display name, enabled status, and ordering.

## Per-client authentication methods

Each client can control which authentication options are available to users.

Available method types:

- Username and password
- Enabled external Identity Providers

This configuration is managed in the Client Branding page under the Authentication Methods tab. The same configuration is used by the live preview and by the actual login page.

## Login experience

The login and authorization screens use the resolved branding for the current client. This includes theme, colors, logo, copy, footer, and enabled authentication methods.

The goal is to provide a professional, client-aware authentication experience similar to enterprise identity providers.

## Consent experience

The authorization flow includes support for a consent screen. Consent is part of the roadmap for becoming a complete OAuth/OIDC identity provider experience and can be expanded to include richer permission descriptions, scope grouping, and client trust indicators.

## Multi-factor authentication

The project includes MFA-related screens and administration areas. MFA support is part of the security foundation and can be expanded with stronger production policies, enrollment rules, recovery options, and administrative reporting.

## Security considerations

- Prefer Authorization Code with PKCE for interactive applications
- Use Client Credentials only for trusted confidential clients
- Rotate client secrets regularly
- Avoid storing plain text secrets
- Keep revoked secrets for auditing
- Use HTTPS in all environments outside local development
- Review redirect URIs carefully
- Limit external providers per client when required by business rules
