using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CleanApiSecurity.Models;

[Table("ApiKeys")]
public sealed class ApiKeyEntry
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(128)]
    public required string Name { get; set; }

    [Required]
    [MaxLength(128)]
    public required string KeyHash { get; set; }

    [Required]
    [MaxLength(12)]
    public required string KeyPrefix { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastUsedAt { get; set; }
}

[Table("UserApiKeys")]
public sealed class UserApiKeyEntry
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [Required]
    [MaxLength(256)]
    public required string ApiKey { get; set; }

    [Required]
    [MaxLength(128)]
    public required string ApiKeyHash { get; set; }

    [Required]
    [MaxLength(12)]
    public required string ApiKeyPrefix { get; set; }

    public DateTimeOffset IssuedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? LastUsedAt { get; set; }

    [Required]
    [MaxLength(32)]
    public string Status { get; set; } = "active";
}

[Table("EndpointPolicies")]
public sealed class EndpointPolicy
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public required string Route { get; set; }

    [Required]
    [MaxLength(10)]
    public required string Method { get; set; }

    public bool IsEnabled { get; set; } = true;

    [MaxLength(300)]
    public string Description { get; set; } = string.Empty;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

[Table("ApiUsageLogs")]
public sealed class ApiUsageLog
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    [MaxLength(200)]
    public string Route { get; set; } = string.Empty;

    [MaxLength(10)]
    public string Method { get; set; } = string.Empty;

    public int StatusCode { get; set; }
    public bool Allowed { get; set; }
    public long DurationMs { get; set; }

    public int? ApiKeyId { get; set; }

    [MaxLength(12)]
    public string ApiKeyPrefix { get; set; } = string.Empty;

    [MaxLength(64)]
    public string ClientIp { get; set; } = string.Empty;

    [MaxLength(300)]
    public string Message { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class CreateApiKeyRequest
{
    public required string Name { get; init; }
}

public sealed class CreateApiKeyResponse
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required string ApiKey { get; init; }
    public required string Prefix { get; init; }
}

public sealed class ApiKeyView
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required string Prefix { get; init; }
    public required bool IsActive { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? LastUsedAt { get; init; }
}

public sealed class SetApiKeyStateRequest
{
    public required bool IsActive { get; init; }
}

public sealed class SetEndpointPolicyRequest
{
    public required string Route { get; init; }
    public required string Method { get; init; }
    public required bool IsEnabled { get; init; }
}

public sealed class EndpointPolicyView
{
    public required int Id { get; init; }
    public required string Route { get; init; }
    public required string Method { get; init; }
    public required bool IsEnabled { get; init; }
    public required string Description { get; init; }
}
