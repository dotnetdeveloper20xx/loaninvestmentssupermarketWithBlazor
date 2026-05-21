using LoanSuperMarket.Domain.Common;

namespace LoanSuperMarket.Domain.Entities.Identity;

public sealed class RecoveryCode : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public bool IsUsed { get; set; }
    public DateTime? UsedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
