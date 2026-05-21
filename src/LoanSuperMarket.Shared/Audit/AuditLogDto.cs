namespace LoanSuperMarket.Shared.Audit;

public sealed class AuditLogDto
{
    public Guid Id { get; set; }

    public string EntityName { get; set; } = string.Empty;

    public Guid? EntityId { get; set; }

    public string Action { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string PerformedBy { get; set; } = string.Empty;

    public DateTime OccurredAtUtc { get; set; }
}