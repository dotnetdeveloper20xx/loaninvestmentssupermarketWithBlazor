namespace LoanSuperMarket.Application.Features.Users.Models;

/// <summary>
/// Summary DTO for user list views with assigned roles, account status, and last login.
/// </summary>
public sealed class UserDto
{
    public string Id { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string FirstName { get; init; } = string.Empty;

    public string LastName { get; init; } = string.Empty;

    public string FullName { get; init; } = string.Empty;

    public IReadOnlyList<string> Roles { get; init; } = [];

    public string AccountStatus { get; init; } = string.Empty;

    public DateTime? LastLoginAtUtc { get; init; }

    public DateTime CreatedAtUtc { get; init; }
}
