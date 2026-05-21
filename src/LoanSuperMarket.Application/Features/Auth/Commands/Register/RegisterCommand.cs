using LoanSuperMarket.Shared.Common;
using MediatR;

namespace LoanSuperMarket.Application.Features.Auth.Commands.Register;

/// <summary>
/// Command to register a new user account.
/// </summary>
public sealed record RegisterCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string UserType,
    string? CompanyName = null) : IRequest<ApiResponse<string>>;
