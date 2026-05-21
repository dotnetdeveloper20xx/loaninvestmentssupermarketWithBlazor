using LoanSuperMarket.Application.Features.Roles.Models;
using LoanSuperMarket.Shared.Common;
using MediatR;

namespace LoanSuperMarket.Application.Features.Roles.Commands.CreateCustomRole;

/// <summary>
/// Command to create a new custom role with granular permissions.
/// </summary>
public sealed record CreateCustomRoleCommand(
    string Name,
    string Description,
    IReadOnlyList<PermissionDto> Permissions) : IRequest<ApiResponse<string>>;
