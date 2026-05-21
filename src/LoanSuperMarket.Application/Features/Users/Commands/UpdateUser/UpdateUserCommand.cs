using LoanSuperMarket.Shared.Common;
using MediatR;

namespace LoanSuperMarket.Application.Features.Users.Commands.UpdateUser;

/// <summary>
/// Command to update an existing user's details and sync their roles.
/// </summary>
public sealed record UpdateUserCommand(
    string UserId,
    string FirstName,
    string LastName,
    IReadOnlyList<string> Roles) : IRequest<ApiResponse<string>>;
