using Microsoft.AspNetCore.Identity;

namespace LoanSuperMarket.Domain.Entities.Identity;

public sealed class CustomRole : IdentityRole
{
    public string? Description { get; set; }
    public bool IsSystemRole { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }

    // Navigation
    public ICollection<RolePermission> Permissions { get; set; } = [];
}
