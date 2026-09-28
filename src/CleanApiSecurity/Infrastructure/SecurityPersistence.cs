using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using CleanApiSecurity.Data;

namespace CleanApiSecurity.Infrastructure;

public static class SecurityPersistence
{
    public static IServiceCollection AddSecurityPersistence(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<SecurityDbContext>(options =>
            options.UseSqlite(connectionString));

        return services;
    }

    public static async Task InitializeDatabaseAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SecurityDbContext>();
        await db.Database.EnsureCreatedAsync();
    }
}
