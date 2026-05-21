using LoanSuperMarket.Domain.Common;
using LoanSuperMarket.Domain.Enums;

namespace LoanSuperMarket.Domain.Entities.Identity;

public sealed class RolePermission : BaseEntity
{
    public string RoleId { get; set; } = string.Empty;
    public PermissionModule Module { get; set; }
    public PermissionAction Action { get; set; }
    public DateTime GrantedAtUtc { get; set; } = DateTime.UtcNow;
    public string? GrantedBy { get; set; }

    // Navigation
    public CustomRole Role { get; set; } = null!;
}
