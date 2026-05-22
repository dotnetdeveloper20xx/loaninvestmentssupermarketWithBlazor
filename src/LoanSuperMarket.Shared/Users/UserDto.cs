namespace LoanSuperMarket.Shared.Users;

/// <summary>
/// Summary DTO for user list views with assigned roles, account status, and last login.
/// </summary>
public sealed class UserDto
{
    public string Id { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public IReadOnlyList<string> Roles { get; set; } = [];

    public string AccountStatus { get; set; } = string.Empty;

    public DateTime? LastLoginAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }
}
