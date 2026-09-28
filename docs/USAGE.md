# Guía de integración

Esta guía explica cómo usar `CleanApiSecurity` en un proyecto nuevo o existente.

## Opción A: Referencia directa (recomendada)

Referencia la librería desde tu proyecto:

```bash
dotnet add reference ../CleanApiSecurity/src/CleanApiSecurity/CleanApiSecurity.csproj
```

## Opción B: Copiar el código

Copia la carpeta `src/CleanApiSecurity` a tu solución y añade el proyecto.

## Paso 1: Configurar el DbContext

En tu `Program.cs`, registra la persistencia (usa SQLite por defecto):

```csharp
using CleanApiSecurity.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// SQLite: base de datos local en un archivo
builder.Services.AddSecurityPersistence("Data Source=security.db");

// Para PostgreSQL (si lo necesitas), instala Npgsql.EntityFrameworkCore.PostgreSQL
// y registra tu propio DbContext apuntando a SecurityDbContext
```

## Paso 2: Registrar los servicios

```csharp
using CleanApiSecurity.Middleware;

builder.Services.AddCleanApiSecurity();
```

## Paso 3: Inicializar la base de datos

```csharp
var app = builder.Build();

await SecurityPersistence.InitializeDatabaseAsync(app.Services);
```

## Paso 4: Declarar rutas protegidas

```csharp
using CleanApiSecurity.Models;
using CleanApiSecurity.Services;

var security = app.Services.GetRequiredService<ApiSecurityService>();

await security.EnsureDefaultsAsync(new List<RoutePolicy>
{
    new() { Route = "/api/users",   Method = "GET",  Description = "Listar usuarios" },
    new() { Route = "/api/users",   Method = "POST", Description = "Crear usuario" },
    new() { Route = "/api/orders",  Method = "GET",  Description = "Listar ordenes" }
});
```

> `EnsureDefaultsAsync` crea las políticas solo si no existen. Si omites una ruta, su endpoint responderá 503 hasta que la crees vía admin.

## Paso 5: Aplicar el middleware

```csharp
app.UseApiSecurity();
app.MapAdminEndpoints();
```

Coloca `UseApiSecurity` antes de tus endpoints protegidos. Es seguro omitirlo en rutas `/api/admin/*` (ya se excluyen).

## Paso 6: Definir tus endpoints

```csharp
app.MapGet("/api/users", () => Results.Ok(new[] { "alice", "bob" }));
```

Cualquier endpoint bajo `/api/*` (excepto `/api/admin/*`) requiere `X-API-Key`.

## Personalización

### Cambiar el prefijo de rutas

```csharp
app.UseApiSecurity(apiPathPrefix: "/v1", adminPathPrefix: "/v1/admin");
```

### Cambiar el nombre del header

```csharp
app.UseApiSecurity(apiKeyHeader: "Authorization"); // poco recomendado
```

### Cambiar el prefijo de las claves

En `ApiSecurityService.CreateKeyAsync`, modifica:

```csharp
var rawKey = $"tsk_{token}";
```

## Uso programático (sin middleware)

Si prefieres validar manualmente en un endpoint:

```csharp
app.MapGet("/api/datos", async (HttpContext ctx, ApiSecurityService security) =>
{
    var rawKey = ctx.Request.Headers["X-API-Key"].ToString();
    var check = await security.ValidateApiKeyAsync(rawKey);

    if (!check.Ok)
        return Results.Unauthorized();

    return Results.Ok(new { valid = true, prefix = check.Prefix });
});
```

## Notas sobre claves de sesión (`usk_`)

El servicio soporta claves de usuario con expiración diaria. Para emitirlas:

```csharp
// Al autenticar un usuario (por ejemplo, con tu propio login JWT/cookie),
// emite o reutiliza una clave diaria:
var sessionKey = await security.EnsureDailyUserApiKeyAsync(userId);
```

Estas claves se validan igual que las `tsk_`, pero se verifican contra `UserApiKeys` y expiran al final del día.

## PostgreSQL en lugar de SQLite

Instala el paquete y registra el DbContext con Npgsql:

```bash
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
```

```csharp
builder.Services.AddDbContext<SecurityDbContext>(options =>
    options.UseNpgsql("Host=localhost;Database=mydb;Username=postgres;Password=..."));
```

Las entidades son provider-agnostic, así que funcionan igual en ambos.

## Preguntas frecuentes

**¿Por qué recibo 503 en mi endpoint?**

La ruta no está en `EndpointPolicies`. Ejecuta `EnsureDefaultsAsync` con la ruta o créala vía `PUT /api/admin/endpoints`.

**¿Dónde se guardan las claves?**

En la tabla `ApiKeys` como hash SHA-256. La clave completa solo se muestra una vez al crearla.

**¿Cómo revoco una clave?**

```bash
curl -X PATCH http://localhost:5000/api/admin/keys/1 \
  -H "Content-Type: application/json" \
  -d '{"isActive":false}'
```
