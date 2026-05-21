namespace LoanSuperMarket.Application.Features.Users.Models;

/// <summary>
/// Detailed DTO for individual user view including credit/capital limits and account status details.
/// </summary>
public sealed class UserDetailDto
{
    public string Id { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string FirstName { get; init; } = string.Empty;

    public string LastName { get; init; } = string.Empty;

    public string FullName { get; init; } = string.Empty;

    public IReadOnlyList<string> Roles { get; init; } = [];

    public string AccountStatus { get; init; } = string.Empty;

    public string? AccountStatusReason { get; init; }

    public DateTime? AccountStatusChangedAtUtc { get; init; }

    public string? AccountStatusChangedBy { get; init; }

    public string? CreditTier { get; init; }

    public decimal? CreditLimit { get; init; }

    public decimal? CapitalLimit { get; init; }

    public string? BlockedActivity { get; init; }

    public bool TwoFactorEnabled { get; init; }

    public bool EmailConfirmed { get; init; }

    public DateTime? LastLoginAtUtc { get; init; }

    public DateTime CreatedAtUtc { get; init; }
}
