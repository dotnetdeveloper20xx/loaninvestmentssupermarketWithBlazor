namespace LoanSuperMarket.Application.Features.Users.Models;

/// <summary>
/// DTO representing a user pending vetting/approval in the CrmManager queue.
/// </summary>
public sealed class VettingItemDto
{
    public string UserId { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string FirstName { get; init; } = string.Empty;

    public string LastName { get; init; } = string.Empty;

    public string FullName { get; init; } = string.Empty;

    public string UserType { get; init; } = string.Empty;

    public string? CompanyName { get; init; }

    public bool EmailConfirmed { get; init; }

    public DateTime RegisteredAtUtc { get; init; }
}
