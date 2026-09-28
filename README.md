# CleanApiSecurity

Plantilla reutilizable de seguridad para APIs en .NET 9. Proporciona autenticación por API Key (`X-API-Key`), políticas de endpoint (enable/disable), y registro de auditoría de uso. Diseñado para copiarse o referenciarse desde cualquier proyecto nuevo que necesite un conector API↔cliente.

## Características

- Generación criptográfica de API keys (prefijo `tsk_`)
- Validación de claves con hash SHA-256 (nunca se guarda la clave en texto plano)
- Políticas por endpoint: activar/desactivar rutas específicas sin tocar código
- Logs de auditoría: cada request registra ruta, método, IP, duración y si fue permitido
- Soporte para claves de sesión por usuario (`usk_`) con expiración diaria
- Middleware plug-and-play con 3 líneas de configuración

## Requisitos

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- SQLite (incluido vía EF Core)

## Estructura del proyecto

```
CleanApiSecurity/
├── CleanApiSecurity.sln
├── global.json
├── src/CleanApiSecurity/           # Librería de clases
│   ├── CleanApiSecurity.csproj
│   ├── Data/
│   │   └── SecurityDbContext.cs    # DbContext con DbSets de seguridad
│   ├── Infrastructure/
│   │   └── SecurityPersistence.cs  # Registro DI + inicialización de BD
│   ├── Middleware/
│   │   ├── ApiSecurityMiddleware.cs # Middleware de validación X-API-Key
│   │   ├── AdminEndpoints.cs       # Endpoints admin (CRUD de keys, políticas, logs)
│   │   └── SecurityServiceExtensions.cs # Extensión para registrar servicios
│   ├── Models/
│   │   ├── ApiSecurityModels.cs    # Entidades EF + DTOs
│   │   └── RoutePolicy.cs         # Modelo para declarar rutas
│   └── Services/
│       └── ApiSecurityService.cs   # Servicio core: CRUD, validación, políticas
├── samples/MinimalApi/             # API de ejemplo
│   ├── MinimalApi.csproj
│   ├── Program.cs
│   └── appsettings.json
└── docs/
    ├── STRUCTURE.md
    ├── USAGE.md
    └── CHANGELOG.md
```

## Inicio rápido

### 1. Ejecutar el proyecto de ejemplo

```bash
cd samples/MinimalApi
dotnet run
```

### 2. Crear una API key

```bash
curl -X POST http://localhost:5000/api/admin/keys \
  -H "Content-Type: application/json" \
  -d '{"name":"mi-aplicacion"}'
```

Respuesta:
```json
{
  "id": 1,
  "name": "mi-aplicacion",
  "apiKey": "tsk_A1B2C3D4E5F6789012345678ABCDEF0123456789ABCDEF",
  "prefix": "tsk_A1B2C3"
}
```

**Guarda la clave completa**, no se volverá a mostrar.

### 3. Habilitar un endpoint

```bash
curl -X PUT http://localhost:5000/api/admin/endpoints \
  -H "Content-Type: application/json" \
  -d '{"route":"/api/hello","method":"GET","isEnabled":true}'
```

### 4. Consumir el endpoint protegido

```bash
curl http://localhost:5000/api/hello \
  -H "X-API-Key: tsk_A1B2C3D4E5F6789012345678ABCDEF0123456789ABCDEF"
```

## Integración en otro proyecto

Ver [docs/USAGE.md](docs/USAGE.md) para la guía paso a paso de integración en un proyecto existente o nuevo.

## API de administración

Todos los endpoints admin son locales (sin API key requerida):

| Método | Ruta | Descripción |
|--------|------|-------------|
| `GET` | `/api/admin/keys` | Lista todas las API keys (no se muestra la clave completa) |
| `POST` | `/api/admin/keys` | Crea una nueva API key (`{"name":"..."}`) |
| `PATCH` | `/api/admin/keys/{id}` | Activa/desactiva una clave (`{"isActive":true}`) |
| `GET` | `/api/admin/endpoints` | Lista políticas de endpoints |
| `PUT` | `/api/admin/endpoints` | Crea/modifica política (`{"route":"...","method":"GET","isEnabled":true}`) |
| `GET` | `/api/admin/logs?take=100` | Historial de uso (últimos N registros) |

## Flujo de validación

```
Cliente → X-API-Key: tsk_xxx → Middleware
                                      ↓
                              ¿Header presente? → No → 401
                                      ↓ Sí
                              SHA-256(key) == BD? → No → 401
                                      ↓ Sí
                              ¿Key activa? → No → 401
                                      ↓ Sí
                              ¿Endpoint habilitado? → No → 503
                                      ↓ Sí
                              Continúa → Registra log
```

## Seguridad

- Las claves se guardan como hash SHA-256, nunca en texto plano
- El prefijo visible (`tsk_A1B2C3`) permite identificación sin exponer la clave
- Cada request se audita con timestamp, IP, ruta y duración
- Las políticas de endpoint se persisten en BD y se evalúan por request

## Formato de claves

| Tipo | Prefijo | Uso |
|------|---------|-----|
| `tsk_` | Task/Admin | Claves permanentes para aplicaciones cliente |
| `usk_` | User Session Key | Claves temporales por sesión de usuario (diarias) |
