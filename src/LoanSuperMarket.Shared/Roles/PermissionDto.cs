namespace LoanSuperMarket.Shared.Roles;

/// <summary>
/// DTO representing a granular permission (module + action combination).
/// </summary>
public sealed class PermissionDto
{
    public string Module { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;
}
