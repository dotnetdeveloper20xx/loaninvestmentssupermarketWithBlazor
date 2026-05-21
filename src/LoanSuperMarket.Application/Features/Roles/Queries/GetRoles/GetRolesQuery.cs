using LoanSuperMarket.Application.Features.Roles.Models;
using MediatR;

namespace LoanSuperMarket.Application.Features.Roles.Queries.GetRoles;

/// <summary>
/// Query to retrieve all roles with their user counts.
/// </summary>
public sealed record GetRolesQuery() : IRequest<IReadOnlyList<RoleDto>>;
