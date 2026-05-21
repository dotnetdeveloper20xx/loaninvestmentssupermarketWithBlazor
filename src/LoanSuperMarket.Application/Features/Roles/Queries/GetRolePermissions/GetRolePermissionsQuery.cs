using LoanSuperMarket.Application.Features.Roles.Models;
using MediatR;

namespace LoanSuperMarket.Application.Features.Roles.Queries.GetRolePermissions;

/// <summary>
/// Query to retrieve all permissions assigned to a specific role.
/// </summary>
public sealed record GetRolePermissionsQuery(string RoleId) : IRequest<IReadOnlyList<PermissionDto>>;
