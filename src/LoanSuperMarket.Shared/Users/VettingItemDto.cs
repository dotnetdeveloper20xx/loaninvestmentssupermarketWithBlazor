namespace LoanSuperMarket.Shared.Users;

/// <summary>
/// DTO representing a user pending vetting/approval in the CrmManager queue.
/// </summary>
public sealed class VettingItemDto
{
    public string UserId { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string UserType { get; set; } = string.Empty;

    public string? CompanyName { get; set; }

    public bool EmailConfirmed { get; set; }

    public DateTime RegisteredAtUtc { get; set; }
}
