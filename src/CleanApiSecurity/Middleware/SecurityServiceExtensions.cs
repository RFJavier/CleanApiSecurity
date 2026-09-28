using Microsoft.Extensions.DependencyInjection;
using CleanApiSecurity.Models;
using CleanApiSecurity.Services;

namespace CleanApiSecurity.Middleware;

public static class SecurityServiceExtensions
{
    public static IServiceCollection AddCleanApiSecurity(this IServiceCollection services)
    {
        services.AddSingleton<ApiSecurityService>();
        return services;
    }
}
