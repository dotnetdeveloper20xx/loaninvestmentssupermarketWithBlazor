namespace LoanSuperMarket.Shared.Roles;

/// <summary>
/// DTO representing a role with its metadata.
/// </summary>
public sealed class RoleDto
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsSystemRole { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public string? CreatedBy { get; set; }

    public int UserCount { get; set; }
}
