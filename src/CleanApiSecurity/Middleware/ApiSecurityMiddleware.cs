using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using CleanApiSecurity.Models;
using CleanApiSecurity.Services;

namespace CleanApiSecurity.Middleware;

public static class ApiSecurityMiddlewareExtensions
{
    public static IApplicationBuilder UseApiSecurity(
        this IApplicationBuilder app,
        string apiPathPrefix = "/api",
        string adminPathPrefix = "/api/admin",
        string apiKeyHeader = "X-API-Key")
    {
        return app.Use(async (ctx, next) =>
        {
            var path = ctx.Request.Path.Value ?? string.Empty;

            if (!path.StartsWith(apiPathPrefix, StringComparison.OrdinalIgnoreCase)
                || path.StartsWith(adminPathPrefix, StringComparison.OrdinalIgnoreCase))
            {
                await next();
                return;
            }

            var security = ctx.RequestServices.GetRequiredService<ApiSecurityService>();
            var method = ctx.Request.Method;
            var normalizedRoute = ApiSecurityService.NormalizeRoute(path);
            var start = DateTimeOffset.UtcNow;

            var enabled = await security.IsEndpointEnabledAsync(normalizedRoute, method);
            if (!enabled)
            {
                ctx.Response.StatusCode = (int)HttpStatusCode.ServiceUnavailable;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.WriteAsync(
                    """{"error":"Endpoint deshabilitado por politica de seguridad."}""");

                await security.LogAsync(new ApiUsageLog
                {
                    Route = normalizedRoute,
                    Method = method,
                    StatusCode = ctx.Response.StatusCode,
                    Allowed = false,
                    DurationMs = (long)(DateTimeOffset.UtcNow - start).TotalMilliseconds,
                    ApiKeyPrefix = string.Empty,
                    ClientIp = ctx.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
                    Message = "Endpoint deshabilitado"
                });
                return;
            }

            var rawKey = ctx.Request.Headers[apiKeyHeader].ToString();
            var keyCheck = await security.ValidateApiKeyAsync(rawKey);

            if (!keyCheck.Ok)
            {
                ctx.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                ctx.Response.ContentType = "application/json";
                await ctx.Response.WriteAsync(
                    """{"error":"Autenticacion invalida. Usa el header X-API-Key con una clave valida."}""");

                await security.LogAsync(new ApiUsageLog
                {
                    Route = normalizedRoute,
                    Method = method,
                    StatusCode = ctx.Response.StatusCode,
                    Allowed = false,
                    DurationMs = (long)(DateTimeOffset.UtcNow - start).TotalMilliseconds,
                    ApiKeyPrefix = rawKey.Length >= 10 ? rawKey[..10] : rawKey,
                    ClientIp = ctx.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
                    Message = "API key invalida o ausente"
                });
                return;
            }

            await next();

            await security.LogAsync(new ApiUsageLog
            {
                Route = normalizedRoute,
                Method = method,
                StatusCode = ctx.Response.StatusCode,
                Allowed = true,
                DurationMs = (long)(DateTimeOffset.UtcNow - start).TotalMilliseconds,
                ApiKeyId = keyCheck.KeyId,
                ApiKeyPrefix = keyCheck.Prefix,
                ClientIp = ctx.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
                Message = $"OK (api-key) prefix={keyCheck.Prefix}"
            });
        });
    }
}
