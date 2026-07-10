# Administrative navigation

The recommended menu structure is:

```text
Overview
├── Dashboard
└── Platform Health

Directory
├── Overview
├── Users
├── Groups
├── Companies
├── Platform Roles
└── Permissions

Applications
├── App Registrations
├── Enterprise Applications
├── Scopes and Claims
└── Token Explorer

Identity & Authentication
├── Identity Providers
├── Multifactor Authentication
├── Passkeys
└── Security Settings

Monitoring & Audit
├── Active Sessions
├── Devices
├── Audit Logs
├── Application Audit
└── Secret Audit

Experience
├── Branding
└── Email Templates
```

## Navigation principles

- Use the global Directory for cross-tenant search.
- Use Tenant Directory only after selecting a company.
- Keep app registration configuration separate from enterprise application access.
- Keep platform roles separate from application roles.
- Keep provider management under Identity, while provider allow-lists belong to the enterprise application.
- Preserve filters and tenant context in breadcrumbs and return links.
- Hide unavailable actions for clarity, but always enforce authorization on the server.
- Use consistent names across menu labels, page headings, routes, documentation, and audit events.

## Contextual links

The Companies page should keep a `Manage users and groups` action. It is a contextual shortcut, not a replacement for the global Directory menu.

The Enterprise Applications page should link to assignments, roles, providers, and tenant-access configuration for the selected application.
