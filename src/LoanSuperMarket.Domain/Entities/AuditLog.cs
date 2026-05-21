using LoanSuperMarket.Domain.Common;

namespace LoanSuperMarket.Domain.Entities;

public sealed class AuditLog : AuditableEntity
{
    private AuditLog()
    {
        EntityName = string.Empty;
        Action = string.Empty;
        Description = string.Empty;
        PerformedBy = string.Empty;
    }

    private AuditLog(
        string entityName,
        Guid? entityId,
        string action,
        string description,
        string performedBy)
    {
        EntityName = entityName;
        EntityId = entityId;
        Action = action;
        Description = description;
        PerformedBy = performedBy;
        OccurredAtUtc = DateTime.UtcNow;
    }

    public string EntityName { get; private set; }

    public Guid? EntityId { get; private set; }

    public string Action { get; private set; }

    public string Description { get; private set; }

    public string PerformedBy { get; private set; }

    public DateTime OccurredAtUtc { get; private set; }

    public static AuditLog Create(
        string entityName,
        Guid? entityId,
        string action,
        string description,
        string performedBy = "System")
    {
        if (string.IsNullOrWhiteSpace(entityName))
        {
            throw new DomainException("Audit entity name is required.");
        }

        if (string.IsNullOrWhiteSpace(action))
        {
            throw new DomainException("Audit action is required.");
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new DomainException("Audit description is required.");
        }

        return new AuditLog(
            entityName.Trim(),
            entityId,
            action.Trim(),
            description.Trim(),
            string.IsNullOrWhiteSpace(performedBy) ? "System" : performedBy.Trim());
    }
}