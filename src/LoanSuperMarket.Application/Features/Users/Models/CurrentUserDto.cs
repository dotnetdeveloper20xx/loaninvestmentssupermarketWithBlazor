namespace LoanSuperMarket.Application.Features.Users.Models;

/// <summary>
/// DTO representing the currently authenticated user's profile and permissions.
/// </summary>
public sealed class CurrentUserDto
{
    public string Id { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string FirstName { get; init; } = string.Empty;

    public string LastName { get; init; } = string.Empty;

    public string FullName { get; init; } = string.Empty;

    public IReadOnlyList<string> Roles { get; init; } = [];

    public IReadOnlyList<string> Permissions { get; init; } = [];

    public string AccountStatus { get; init; } = string.Empty;

    public bool TwoFactorEnabled { get; init; }
}
