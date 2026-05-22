namespace LoanSuperMarket.Shared.Dashboard;

public sealed class UpcomingPaymentDto
{
    public Guid ScheduleId { get; set; }

    public DateTime DueDate { get; set; }

    public decimal Amount { get; set; }

    public string ProductTitle { get; set; } = string.Empty;
}
