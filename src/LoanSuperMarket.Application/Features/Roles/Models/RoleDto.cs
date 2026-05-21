namespace LoanSuperMarket.Application.Features.Roles.Models;

/// <summary>
/// DTO representing a role with its metadata.
/// </summary>
public sealed class RoleDto
{
    public string Id { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    public bool IsSystemRole { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public string? CreatedBy { get; init; }

    public int UserCount { get; init; }
}
