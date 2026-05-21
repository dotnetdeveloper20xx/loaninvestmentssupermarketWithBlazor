namespace LoanSuperMarket.Application.Features.Roles.Models;

/// <summary>
/// DTO representing a granular permission (module + action combination).
/// </summary>
public sealed class PermissionDto
{
    public string Module { get; init; } = string.Empty;

    public string Action { get; init; } = string.Empty;
}
