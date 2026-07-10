# Auth2Demo documentation

This folder contains the technical and operational documentation for Auth2Demo.

## Architecture and development

- [Architecture](architecture.md): layers, dependencies, conventions, and request flow.
- [Database](database.md): persistence model, table groups, delete behavior, and migration workflow.
- [Configuration](configuration.md): connection strings, external providers, cookies, localization, and secret handling.
- [Testing](testing.md): test projects, recommended coverage, and validation workflow.

## Identity platform

- [Authentication](authentication.md): local login, external login, OpenID Connect, password policy, and profile flows.
- [Applications](applications.md): app registrations, enterprise applications, scopes, secrets, roles, and assignments.
- [Tenant governance](tenant-governance.md): companies, memberships, groups, providers, and access evaluation.
- [Tenant directory](tenant-directory.md): global and tenant-scoped administration workflows.
- [Security](security.md): security controls, threat considerations, and production hardening.

## Administration and operations

- [Administration](administration.md): administrative workspaces and responsibilities.
- [Administrative navigation](admin-navigation.md): menu structure and routing conventions.
- [Deployment](deployment.md): local, IIS/reverse proxy, database, keys, health, and operational checklist.
- [Localization](localization.md): supported cultures and resource conventions.
- [Professional roadmap](professional-roadmap.md): current maturity and recommended next milestones.

## Documentation rules

When functionality changes:

1. Update the related document in this folder.
2. Update the root `README.md` when the feature affects setup or headline capabilities.
3. Document schema changes in `database.md`.
4. Document new settings in `configuration.md`.
5. Document new administrative routes in `administration.md` and `admin-navigation.md`.
6. Keep statements aligned with implemented code; planned capabilities belong only in the roadmap.
