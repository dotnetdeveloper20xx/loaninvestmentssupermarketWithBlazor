using LoanSuperMarket.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace LoanSuperMarket.Domain.Entities.Identity;

public sealed class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public AccountStatus AccountStatus { get; set; } = AccountStatus.PendingApproval;
    public string? AccountStatusReason { get; set; }
    public DateTime? AccountStatusChangedAtUtc { get; set; }
    public string? AccountStatusChangedBy { get; set; }
    public CreditTier? CreditTier { get; set; }
    public decimal? CreditLimit { get; set; }
    public decimal? CapitalLimit { get; set; }
    public string? BlockedActivity { get; set; } // "Borrowing", "Lending", "Both"
    public bool TwoFactorSetupComplete { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAtUtc { get; set; }

    public string FullName => $"{FirstName} {LastName}";

    // Navigation properties
    public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
    public ICollection<UserSession> Sessions { get; set; } = [];
}
