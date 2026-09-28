namespace CleanApiSecurity.Models;

public sealed class RoutePolicy
{
    public required string Route { get; init; }
    public required string Method { get; init; }
    public required string Description { get; init; }
}
