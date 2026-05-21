using LoanSuperMarket.Domain.Common;

namespace LoanSuperMarket.Domain.Entities.Identity;

public sealed class RefreshToken : BaseEntity
{
    public string Token { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public string? ReplacedByToken { get; set; }
    public string? RevokedReason { get; set; }
    public bool IsRememberMe { get; set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAtUtc;
    public bool IsRevoked => RevokedAtUtc is not null;
    public bool IsActive => !IsExpired && !IsRevoked;

    // Navigation
    public ApplicationUser User { get; set; } = null!;
}
