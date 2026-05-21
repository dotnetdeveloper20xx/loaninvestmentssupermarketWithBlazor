using LoanSuperMarket.Domain.Common;

namespace LoanSuperMarket.Domain.Entities.Identity;

public sealed class UserSession : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string RefreshTokenId { get; set; } = string.Empty;
    public string? DeviceType { get; set; }
    public string? IpAddress { get; set; }
    public string? Browser { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime LastActivityAtUtc { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    // Navigation
    public ApplicationUser User { get; set; } = null!;
}
