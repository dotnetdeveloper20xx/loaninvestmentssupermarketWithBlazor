namespace LoanSuperMarket.Domain.Common;

public abstract class AuditableEntity : BaseEntity
{
    public DateTime CreatedAtUtc { get; private set; } = DateTime.UtcNow;

    public string? CreatedBy { get; private set; }

    public DateTime? UpdatedAtUtc { get; private set; }

    public string? UpdatedBy { get; private set; }

    public void MarkCreated(string? createdBy = null)
    {
        CreatedAtUtc = DateTime.UtcNow;
        CreatedBy = createdBy;
    }

    public void MarkUpdated(string? updatedBy = null)
    {
        UpdatedAtUtc = DateTime.UtcNow;
        UpdatedBy = updatedBy;
    }
}