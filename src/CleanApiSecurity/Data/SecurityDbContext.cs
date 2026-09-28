using Microsoft.EntityFrameworkCore;
using CleanApiSecurity.Models;

namespace CleanApiSecurity.Data;

public class SecurityDbContext : DbContext
{
    public SecurityDbContext(DbContextOptions<SecurityDbContext> options) : base(options) { }

    public DbSet<ApiKeyEntry> ApiKeys => Set<ApiKeyEntry>();
    public DbSet<UserApiKeyEntry> UserApiKeys => Set<UserApiKeyEntry>();
    public DbSet<EndpointPolicy> EndpointPolicies => Set<EndpointPolicy>();
    public DbSet<ApiUsageLog> ApiUsageLogs => Set<ApiUsageLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ApiKeyEntry>(entity =>
        {
            entity.HasIndex(e => e.KeyHash).IsUnique();
            entity.HasIndex(e => e.IsActive);
        });

        modelBuilder.Entity<UserApiKeyEntry>(entity =>
        {
            entity.HasIndex(e => e.ApiKeyHash).IsUnique();
            entity.HasIndex(e => new { e.UserId, e.Status });
        });

        modelBuilder.Entity<EndpointPolicy>(entity =>
        {
            entity.HasIndex(e => new { e.Route, e.Method }).IsUnique();
        });

        modelBuilder.Entity<ApiUsageLog>(entity =>
        {
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.ApiKeyPrefix);
        });
    }
}
