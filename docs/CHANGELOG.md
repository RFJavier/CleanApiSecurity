# Changelog

Todos los cambios notables de este proyecto se documentan en este archivo.

El formato sigue [Keep a Changelog](https://keepachangelog.com/es-ES/1.0.0/) y el versionado [SemVer](https://semver.org/lang/es/).

## [1.0.0] - 2026-09-28

### Añadido

- Librería de clases `CleanApiSecurity` para .NET 9
- `ApiSecurityService`: generación, validación y administración de API keys
  - Generación criptográfica con `RandomNumberGenerator` (prefijo `tsk_`)
  - Validación por hash SHA-256 (claves nunca en texto plano)
  - Soporte para claves de sesión por usuario (`usk_`) con expiración diaria
  - CRUD de claves (crear, listar, activar/desactivar)
- `SecurityDbContext`: DbContext EF Core con las tablas de seguridad
  - `ApiKeys`, `UserApiKeys`, `EndpointPolicies`, `ApiUsageLogs`
- Middleware `UseApiSecurity` para validación de `X-API-Key`
  - Rechazo de endpoints deshabilitados (503)
  - Rechazo de claves inválidas (401)
  - Registro de auditoría por request
- Endpoints admin plug-and-play (`MapAdminEndpoints`)
  - CRUD de API keys, políticas de endpoints y logs de uso
- Políticas por endpoint: activar/desactivar rutas sin recompilar
- Proyecto de ejemplo `MinimalApi` demostrando la integración
- Documentación: `README.md`, `STRUCTURE.md`, `USAGE.md`, `CHANGELOG.md`
