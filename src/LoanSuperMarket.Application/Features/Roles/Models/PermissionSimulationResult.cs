namespace LoanSuperMarket.Application.Features.Roles.Models;

/// <summary>
/// Result of simulating the effective permissions for a user based on all assigned roles.
/// </summary>
public sealed class PermissionSimulationResult
{
    public string UserId { get; init; } = string.Empty;

    public string UserEmail { get; init; } = string.Empty;

    public IReadOnlyList<string> AssignedRoles { get; init; } = [];

    public IReadOnlyList<PermissionDto> EffectivePermissions { get; init; } = [];

    public IReadOnlyList<string> EffectivePolicies { get; init; } = [];
}
