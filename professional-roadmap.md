# Roadmap para virar um Auth0 próprio

Já implementado neste laboratório:

- Identity Provider OIDC/OAuth2.
- Gestão de usuários, roles, clients, secrets e scopes.
- Authorization Code + PKCE, Refresh Token e Client Credentials.
- Consentimento e UserInfo.

Próximos passos recomendados para produção:

1. E-mail real: confirmação, recuperação de senha e troca de senha.
2. MFA/TOTP/WebAuthn/passkeys.
3. Auditoria completa de eventos de segurança.
4. Logs estruturados com correlation id.
5. Rotação de secrets e expiração automática.
6. Tela de gerenciamento de claims por client/API.
7. Certificados reais para assinatura/encriptação de tokens.
8. Rate limit nos endpoints de login e token.
9. Detecção de login suspeito por IP/dispositivo.
10. Administração multi-tenant, caso precise atender múltiplos produtos.
