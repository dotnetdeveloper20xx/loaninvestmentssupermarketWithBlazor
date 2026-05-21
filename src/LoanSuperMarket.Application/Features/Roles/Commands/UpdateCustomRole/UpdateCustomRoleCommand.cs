using LoanSuperMarket.Application.Features.Roles.Models;
using LoanSuperMarket.Shared.Common;
using MediatR;

namespace LoanSuperMarket.Application.Features.Roles.Commands.UpdateCustomRole;

/// <summary>
/// Command to update an existing custom role's description and permissions.
/// </summary>
public sealed record UpdateCustomRoleCommand(
    string RoleId,
    string Description,
    IReadOnlyList<PermissionDto> Permissions) : IRequest<ApiResponse<string>>;
