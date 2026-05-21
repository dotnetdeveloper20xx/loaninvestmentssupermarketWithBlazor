namespace LoanSuperMarket.Application.Features.Auth.Models;

/// <summary>
/// Request model for user registration via the identity service.
/// </summary>
public sealed record RegisterUserRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string UserType,
    string? CompanyName = null);
