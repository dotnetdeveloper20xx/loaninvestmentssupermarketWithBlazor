using LoanSuperMarket.Shared.Common;
using MediatR;

namespace LoanSuperMarket.Application.Features.Users.Commands.CreateUser;

/// <summary>
/// Command to create a new user account with specified roles (admin action).
/// </summary>
public sealed record CreateUserCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    IReadOnlyList<string> Roles) : IRequest<ApiResponse<string>>;
