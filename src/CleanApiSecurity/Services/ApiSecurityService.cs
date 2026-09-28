using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using CleanApiSecurity.Data;
using CleanApiSecurity.Models;

namespace CleanApiSecurity.Services;

public sealed class ApiSecurityService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public ApiSecurityService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task EnsureDefaultsAsync(IReadOnlyList<RoutePolicy> routes)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SecurityDbContext>();

        foreach (var route in routes)
        {
            var exists = await db.EndpointPolicies.AnyAsync(p =>
                p.Route == route.Route && p.Method == route.Method);

            if (!exists)
            {
                db.EndpointPolicies.Add(new EndpointPolicy
                {
                    Route = route.Route,
                    Method = route.Method,
                    Description = route.Description
                });
            }
        }

        await db.SaveChangesAsync();
    }

    public async Task<bool> IsEndpointEnabledAsync(string route, string method)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SecurityDbContext>();

        var normalized = NormalizeRoute(route);
        var policy = await db.EndpointPolicies.FirstOrDefaultAsync(p =>
            p.Route == normalized && p.Method == method.ToUpperInvariant());

        return policy?.IsEnabled ?? false;
    }

    public async Task<(bool Ok, int? KeyId, string Prefix, Guid? UserId, bool IsUserScoped)> ValidateApiKeyAsync(
        string? rawKey,
        Guid? expectedUserId = null)
    {
        if (string.IsNullOrWhiteSpace(rawKey))
            return (false, null, string.Empty, null, false);

        var hash = Sha256(rawKey.Trim());
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SecurityDbContext>();

        var now = DateTimeOffset.UtcNow;

        var userKeyQuery = db.UserApiKeys
            .Where(k => k.ApiKeyHash == hash && k.Status == "active");

        if (expectedUserId.HasValue)
            userKeyQuery = userKeyQuery.Where(k => k.UserId == expectedUserId.Value);

        var userCandidates = await userKeyQuery.ToListAsync();
        var userEntry = userCandidates
            .Where(k => k.ExpiresAt > now)
            .OrderByDescending(k => k.IssuedAt)
            .FirstOrDefault();

        if (userEntry is not null)
        {
            userEntry.LastUsedAt = now;
            await db.SaveChangesAsync();
            return (true, userEntry.Id, userEntry.ApiKeyPrefix, userEntry.UserId, true);
        }

        var entry = await db.ApiKeys.FirstOrDefaultAsync(k =>
            k.KeyHash == hash && k.IsActive);

        if (entry is null)
            return (false, null, string.Empty, null, false);

        entry.LastUsedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync();

        return (true, entry.Id, entry.KeyPrefix, null, false);
    }

    public async Task<CreateApiKeyResponse> CreateKeyAsync(string name)
    {
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(24));
        var rawKey = $"tsk_{token}";

        var entry = new ApiKeyEntry
        {
            Name = name,
            KeyHash = Sha256(rawKey),
            KeyPrefix = rawKey[..10],
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SecurityDbContext>();

        db.ApiKeys.Add(entry);
        await db.SaveChangesAsync();

        return new CreateApiKeyResponse
        {
            Id = entry.Id,
            Name = entry.Name,
            ApiKey = rawKey,
            Prefix = entry.KeyPrefix
        };
    }

    public async Task<IReadOnlyList<ApiKeyView>> GetKeysAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SecurityDbContext>();

        var keys = await db.ApiKeys
            .Select(k => new ApiKeyView
            {
                Id = k.Id,
                Name = k.Name,
                Prefix = k.KeyPrefix,
                IsActive = k.IsActive,
                CreatedAt = k.CreatedAt,
                LastUsedAt = k.LastUsedAt
            })
            .ToListAsync();

        return keys
            .OrderByDescending(k => k.CreatedAt)
            .ToList();
    }

    public async Task<bool> SetKeyStateAsync(int id, bool isActive)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SecurityDbContext>();

        var key = await db.ApiKeys.FindAsync(id);
        if (key is null) return false;

        key.IsActive = isActive;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<IReadOnlyList<EndpointPolicyView>> GetEndpointPoliciesAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SecurityDbContext>();

        return await db.EndpointPolicies
            .OrderBy(p => p.Route)
            .ThenBy(p => p.Method)
            .Select(p => new EndpointPolicyView
            {
                Id = p.Id,
                Route = p.Route,
                Method = p.Method,
                IsEnabled = p.IsEnabled,
                Description = p.Description
            })
            .ToListAsync();
    }

    public async Task<bool> SetEndpointPolicyAsync(string route, string method, bool enabled)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SecurityDbContext>();

        var normalized = NormalizeRoute(route);
        var policy = await db.EndpointPolicies.FirstOrDefaultAsync(p =>
            p.Route == normalized && p.Method == method.ToUpperInvariant());

        if (policy is null)
        {
            policy = new EndpointPolicy
            {
                Route = normalized,
                Method = method.ToUpperInvariant(),
                IsEnabled = enabled,
                Description = "Custom",
                UpdatedAt = DateTimeOffset.UtcNow
            };
            db.EndpointPolicies.Add(policy);
        }
        else
        {
            policy.IsEnabled = enabled;
            policy.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync();
        return true;
    }

    public async Task LogAsync(ApiUsageLog log)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SecurityDbContext>();

        db.ApiUsageLogs.Add(log);
        await db.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<ApiUsageLog>> GetLogsAsync(int take = 200)
    {
        take = Math.Clamp(take, 1, 2000);

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SecurityDbContext>();

        var logs = await db.ApiUsageLogs.ToListAsync();

        return logs
            .OrderByDescending(l => l.CreatedAt)
            .Take(take)
            .ToList();
    }

    public static string NormalizeRoute(string path)
    {
        if (path.StartsWith("/api/scan/") && path.Contains("/files/"))
            return "/api/scan/files";

        if (path.StartsWith("/api/scan/") && path.Count(c => c == '/') == 3)
            return "/api/scan/{id}";

        if (path.StartsWith("/api/devices/") && path.Count(c => c == '/') == 3)
            return "/api/devices";

        if (path.StartsWith("/api/config/"))
            return "/api/config";

        return path;
    }

    public static string Sha256(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }
}
