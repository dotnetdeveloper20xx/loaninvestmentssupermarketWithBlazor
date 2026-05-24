namespace LoanSuperMarket.Shared.Dashboard;

public sealed class CollectionItemDto
{
    public Guid ScheduleId { get; set; }

    public string BorrowerName { get; set; } = string.Empty;

    public string LenderName { get; set; } = string.Empty;

    public decimal OutstandingAmount { get; set; }

    public int MissedInstallments { get; set; }

    public DateTime DefaultDate { get; set; }

    public string CollectionStatus { get; set; } = "New";

    public string? LastContactNote { get; set; }

    public DateTime? LastContactDate { get; set; }
}
