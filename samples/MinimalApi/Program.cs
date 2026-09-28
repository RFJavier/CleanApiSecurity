using CleanApiSecurity.Infrastructure;
using CleanApiSecurity.Middleware;
using CleanApiSecurity.Models;
using CleanApiSecurity.Services;

var builder = WebApplication.CreateBuilder(args);

var dbPath = Path.Combine(builder.Environment.ContentRootPath, "security.db");
builder.Services.AddSecurityPersistence($"Data Source={dbPath}");
builder.Services.AddCleanApiSecurity();

var app = builder.Build();

await SecurityPersistence.InitializeDatabaseAsync(app.Services);

var defaultRoutes = new List<RoutePolicy>
{
    new() { Route = "/api/weather", Method = "GET", Description = "Weather forecast" },
    new() { Route = "/api/hello", Method = "GET", Description = "Hello world" }
};

var security = app.Services.GetRequiredService<ApiSecurityService>();
await security.EnsureDefaultsAsync(defaultRoutes);

app.UseApiSecurity();
app.MapAdminEndpoints();

app.MapGet("/api/weather", () =>
{
    var summaries = new[] { "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching" };
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new
        {
            Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = summaries[Random.Shared.Next(summaries.Length)]
        });
    return Results.Ok(forecast);
});

app.MapGet("/api/hello", () => Results.Ok(new { message = "Hello from CleanApiSecurity!" }));

app.MapGet("/", () => Results.Ok(new
{
    project = "CleanApiSecurity Sample",
    adminEndpoints = new[]
    {
        "GET  /api/admin/keys       - Listar API keys",
        "POST /api/admin/keys       - Crear API key",
        "PATCH /api/admin/keys/{id}  - Activar/desactivar API key",
        "GET  /api/admin/endpoints  - Listar politicas de endpoints",
        "PUT  /api/admin/endpoints  - Modificar politica de endpoint",
        "GET  /api/admin/logs       - Ver logs de uso"
    },
    protectedEndpoints = new[]
    {
        "GET /api/weather - Requiere X-API-Key (debe estar habilitado en politicas)",
        "GET /api/hello   - Requiere X-API-Key (debe estar habilitado en politicas)"
    },
    quickStart = "1. POST /api/admin/keys con {\"name\":\"mi-app\"} → obtienes la clave. 2. PUT /api/admin/endpoints con {\"route\":\"/api/hello\",\"method\":\"GET\",\"isEnabled\":true}. 3. GET /api/hello con header X-API-Key: tsk_..."
}));

app.Run();
