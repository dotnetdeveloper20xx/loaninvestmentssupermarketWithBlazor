using LoanSuperMarket.Application.Features.Roles.Models;
using MediatR;

namespace LoanSuperMarket.Application.Features.Roles.Queries.SimulatePermissions;

/// <summary>
/// Query to simulate the effective permissions for a user based on all assigned roles.
/// </summary>
public sealed record SimulatePermissionsQuery(string UserId) : IRequest<PermissionSimulationResult>;
