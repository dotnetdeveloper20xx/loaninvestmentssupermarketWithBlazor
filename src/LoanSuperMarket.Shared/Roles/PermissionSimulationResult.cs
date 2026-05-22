namespace LoanSuperMarket.Shared.Roles;

/// <summary>
/// Result of simulating the effective permissions for a user based on all assigned roles.
/// </summary>
public sealed class PermissionSimulationResult
{
    public string UserId { get; set; } = string.Empty;

    public string UserEmail { get; set; } = string.Empty;

    public IReadOnlyList<string> AssignedRoles { get; set; } = [];

    public IReadOnlyList<PermissionDto> EffectivePermissions { get; set; } = [];

    public IReadOnlyList<string> EffectivePolicies { get; set; } = [];
}
