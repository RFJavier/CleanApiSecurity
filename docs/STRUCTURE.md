# Arquitectura y estructura

## Resumen

`CleanApiSecurity` es una librería de clases .NET 9 que encapsula la autenticación por API key. Está dividida en capas claras para que sea fácil de copiar y adaptar.

## Capas

| Capa | Carpeta | Responsabilidad |
|------|---------|-----------------|
| Modelos | `Models/` | Entidades EF Core y DTOs |
| Datos | `Data/` | `SecurityDbContext` con DbSets de seguridad |
| Infraestructura | `Infrastructure/` | Registro DI y arranque de BD |
| Servicios | `Services/` | `ApiSecurityService` (lógica core) |
| Middleware | `Middleware/` | Validación de requests + endpoints admin |

## Diagrama de dependencias

```
samples/MinimalApi (WebApplication)
        │
        ├── referencias a src/CleanApiSecurity
        │
        ▼
src/CleanApiSecurity
        │
        ├── Models (sin dependencias)
        ├── Data (depende de Models)
        ├── Services (depende de Data + Models)
        ├── Infrastructure (depende de Data)
        └── Middleware (depende de Services + Models)
```

## Modelos de datos

### ApiKeyEntry (tabla `ApiKeys`)

Claves permanentes para aplicaciones cliente.

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `Id` | int | PK autoincremental |
| `Name` | string | Nombre descriptivo |
| `KeyHash` | string | SHA-256 de la clave completa |
| `KeyPrefix` | string | Primeros 10 caracteres visibles |
| `IsActive` | bool | Si la clave está habilitada |
| `CreatedAt` | DateTimeOffset | Fecha de creación |
| `LastUsedAt` | DateTimeOffset? | Último uso |

### UserApiKeyEntry (tabla `UserApiKeys`)

Claves de sesión por usuario (expiran diariamente).

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `Id` | int | PK autoincremental |
| `UserId` | Guid | Usuario dueño de la clave |
| `ApiKey` | string | Clave emitida (trazabilidad) |
| `ApiKeyHash` | string | SHA-256 |
| `ApiKeyPrefix` | string | Prefijo visible |
| `IssuedAt` | DateTimeOffset | Emisión |
| `ExpiresAt` | DateTimeOffset | Expiración |
| `Status` | string | `active` / `revoked` |

### EndpointPolicy (tabla `EndpointPolicies`)

Política por endpoint para activar/desactivar rutas.

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `Id` | int | PK |
| `Route` | string | Ruta normalizada |
| `Method` | string | HTTP method (GET, POST...) |
| `IsEnabled` | bool | Si el endpoint responde |
| `Description` | string | Texto libre |
| `UpdatedAt` | DateTimeOffset | Última modificación |

### ApiUsageLog (tabla `ApiUsageLogs`)

Auditoría de uso.

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `Id` | long | PK |
| `Route` | string | Ruta del request |
| `Method` | string | Método HTTP |
| `StatusCode` | int | Status devuelto |
| `Allowed` | bool | Si fue permitido |
| `DurationMs` | long | Duración |
| `ApiKeyId` | int? | ID de la clave usada |
| `ApiKeyPrefix` | string | Prefijo de la clave |
| `ClientIp` | string | IP del cliente |
| `Message` | string | Detalle |
| `CreatedAt` | DateTimeOffset | Timestamp |

## Flujo de un request protegido

1. El cliente envía `X-API-Key: tsk_xxx`
2. El middleware `UseApiSecurity` intercepta rutas `/api/*` (excepto `/api/admin/*`)
3. Normaliza la ruta (ej. `/api/scan/123` → `/api/scan/{id}`)
4. Verifica si el endpoint está habilitado (`EndpointPolicies`)
5. Si está deshabilitado → responde 503 y registra log
6. Valida la clave contra SHA-256 en BD
7. Si es inválida → responde 401 y registra log
8. Si es válida → pasa al siguiente middleware
9. Al terminar, registra el log de éxito con duración

## Normalización de rutas

`ApiSecurityService.NormalizeRoute` convierte rutas parametrizadas a una forma canónica para que una sola política cubra múltiples instancias:

```
/api/scan/123          → /api/scan/{id}
/api/scan/123/files/a  → /api/scan/files
/api/devices/5         → /api/devices
/api/config/scanner    → /api/config
```

Para adaptar a tu dominio, modifica este método estático.

## Decisiones de diseño

- **Singleton**: `ApiSecurityService` se registra como singleton y crea scopes internos para acceder al DbContext (patrón scope-per-operation).
- **Hash en BD**: nunca se almacena la clave en texto plano.
- **Prefijo visible**: permite identificar claves en logs sin exponerlas.
- **Políticas en BD**: permite deshabilitar endpoints sin redeploy.
- **Middleware configurable**: `apiPathPrefix`, `adminPathPrefix` y `apiKeyHeader` parametrizables.
