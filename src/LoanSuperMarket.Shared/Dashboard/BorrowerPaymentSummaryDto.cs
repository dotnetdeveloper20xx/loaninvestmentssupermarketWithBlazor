namespace LoanSuperMarket.Shared.Dashboard;

public sealed class BorrowerPaymentSummaryDto
{
    public decimal TotalInterestPaid { get; set; }

    public decimal TotalPrincipalPaid { get; set; }

    public List<PaymentHistoryEntry> PaymentHistory { get; set; } = [];

    public List<UpcomingPaymentEntry> UpcomingPayments { get; set; } = [];
}

public sealed class PaymentHistoryEntry
{
    public Guid ScheduleId { get; set; }

    public string ProductTitle { get; set; } = string.Empty;

    public int InstallmentNumber { get; set; }

    public DateTime DueDate { get; set; }

    public DateTime? PaidDate { get; set; }

    public decimal PaidAmount { get; set; }

    public string Status { get; set; } = string.Empty;
}

public sealed class UpcomingPaymentEntry
{
    public Guid ScheduleId { get; set; }

    public string ProductTitle { get; set; } = string.Empty;

    public DateTime DueDate { get; set; }

    public decimal Amount { get; set; }

    public int InstallmentNumber { get; set; }
}
