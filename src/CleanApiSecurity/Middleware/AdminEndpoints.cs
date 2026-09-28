using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using CleanApiSecurity.Models;
using CleanApiSecurity.Services;

namespace CleanApiSecurity.Middleware;

public static class AdminEndpoints
{
    public static WebApplication MapAdminEndpoints(this WebApplication app)
    {
        app.MapGet("/api/admin/keys", async (ApiSecurityService security) =>
            Results.Ok(await security.GetKeysAsync()));

        app.MapPost("/api/admin/keys", async (CreateApiKeyRequest request, ApiSecurityService security) =>
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return Results.BadRequest(new { error = "Name es requerido." });

            var key = await security.CreateKeyAsync(request.Name.Trim());
            return Results.Ok(key);
        });

        app.MapPatch("/api/admin/keys/{id:int}", async (
            int id,
            SetApiKeyStateRequest request,
            ApiSecurityService security) =>
        {
            var ok = await security.SetKeyStateAsync(id, request.IsActive);
            return ok ? Results.Ok(new { updated = true }) : Results.NotFound();
        });

        app.MapGet("/api/admin/endpoints", async (ApiSecurityService security) =>
            Results.Ok(await security.GetEndpointPoliciesAsync()));

        app.MapPut("/api/admin/endpoints", async (
            SetEndpointPolicyRequest request,
            ApiSecurityService security) =>
        {
            if (string.IsNullOrWhiteSpace(request.Route) || string.IsNullOrWhiteSpace(request.Method))
                return Results.BadRequest(new { error = "Route y Method son requeridos." });

            await security.SetEndpointPolicyAsync(request.Route, request.Method, request.IsEnabled);
            return Results.Ok(new { saved = true });
        });

        app.MapGet("/api/admin/logs", async (ApiSecurityService security, int take = 200) =>
            Results.Ok(await security.GetLogsAsync(take)));

        return app;
    }
}
