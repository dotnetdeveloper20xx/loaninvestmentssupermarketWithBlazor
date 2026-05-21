using LoanSuperMarket.Shared.Common;
using MediatR;

namespace LoanSuperMarket.Application.Features.Users.Commands.AssignRole;

/// <summary>
/// Command to assign a role to a user.
/// </summary>
public sealed record AssignRoleCommand(
    string UserId,
    string RoleName) : IRequest<ApiResponse<string>>;
