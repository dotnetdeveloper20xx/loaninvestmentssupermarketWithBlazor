using LoanSuperMarket.Shared.Common;
using MediatR;

namespace LoanSuperMarket.Application.Features.Users.Commands.RemoveRole;

/// <summary>
/// Command to remove a role from a user.
/// </summary>
public sealed record RemoveRoleCommand(
    string UserId,
    string RoleName) : IRequest<ApiResponse<string>>;
