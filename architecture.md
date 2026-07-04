# Arquitetura

## Domain

Não depende de EF, Identity, OpenIddict ou ASP.NET Core.
Contém enums, entidades e regras próprias do negócio.

## Application

Contém contratos e casos de uso. No início está leve de propósito.

## Infrastructure

Contém EF Core, Identity, OpenIddict, DbContext, configurações e serviços técnicos.

## Web

É o Authorization Server: expõe login, logout e endpoints OIDC/OAuth2.

Endpoints principais:

```txt
/connect/authorize
/connect/token
/connect/userinfo
/connect/logout
```
