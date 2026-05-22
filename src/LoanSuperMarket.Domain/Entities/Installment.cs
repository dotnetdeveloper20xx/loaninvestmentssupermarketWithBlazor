using LoanSuperMarket.Domain.Common;
using LoanSuperMarket.Domain.Enums;

namespace LoanSuperMarket.Domain.Entities;

public sealed class Installment : AuditableEntity
{
    private Installment() { }

    internal Installment(
        int installmentNumber,
        DateTime dueDate,
        decimal principalPortion,
        decimal interestPortion,
        decimal remainingBalance)
    {
        InstallmentNumber = installmentNumber;
        DueDate = dueDate;
        PrincipalPortion = principalPortion;
        InterestPortion = interestPortion;
        TotalAmount = principalPortion + interestPortion;
        RemainingBalance = remainingBalance;
        Status = InstallmentStatus.Pending;
        PaidAmount = 0;
        LateFeeAmount = 0;
    }

    public Guid RepaymentScheduleId { get; private set; }

    public int InstallmentNumber { get; private set; }

    public DateTime DueDate { get; private set; }

    public decimal PrincipalPortion { get; private set; }

    public decimal InterestPortion { get; private set; }

    public decimal TotalAmount { get; private set; }

    public decimal RemainingBalance { get; private set; }

    public InstallmentStatus Status { get; private set; }

    public decimal PaidAmount { get; private set; }

    public DateTime? PaidDate { get; private set; }

    public decimal LateFeeAmount { get; private set; }

    public string? Notes { get; private set; }

    public bool ReminderSent { get; private set; }

    public bool LateNoticeSent { get; private set; }

    public void RecordFullPayment(DateTime paymentDate)
    {
        if (Status == InstallmentStatus.Paid)
        {
            throw new DomainException("Installment is already fully paid.");
        }

        var totalOwed = TotalAmount + LateFeeAmount;
        PaidAmount = totalOwed;
        PaidDate = paymentDate;
        Status = InstallmentStatus.Paid;
        MarkUpdated();
    }

    public void RecordPartialPayment(decimal amount, DateTime paymentDate)
    {
        if (amount <= 0)
        {
            throw new DomainException("Payment amount must be greater than zero.");
        }

        if (Status == InstallmentStatus.Paid)
        {
            throw new DomainException("Installment is already fully paid.");
        }

        var totalOwed = TotalAmount + LateFeeAmount;
        var newPaidAmount = PaidAmount + amount;

        if (newPaidAmount > totalOwed)
        {
            throw new DomainException(
                $"Payment of {amount:N2} would exceed the total owed of {totalOwed:N2}. " +
                $"Maximum additional payment allowed is {totalOwed - PaidAmount:N2}.");
        }

        PaidAmount = newPaidAmount;
        PaidDate = paymentDate;

        if (PaidAmount >= totalOwed)
        {
            Status = InstallmentStatus.Paid;
        }
        else
        {
            Status = InstallmentStatus.PartiallyPaid;
        }

        MarkUpdated();
    }

    public void MarkLate(decimal lateFeePercentage)
    {
        if (Status != InstallmentStatus.Pending && Status != InstallmentStatus.PartiallyPaid)
        {
            throw new DomainException(
                $"Cannot mark installment as late when status is '{Status}'. " +
                "Only Pending or PartiallyPaid installments can be marked late.");
        }

        Status = InstallmentStatus.Late;
        LateFeeAmount = decimal.Round((TotalAmount - PaidAmount) * lateFeePercentage, 2);
        MarkUpdated();
    }

    public void MarkMissed()
    {
        if (Status != InstallmentStatus.Late)
        {
            throw new DomainException(
                $"Cannot mark installment as missed when status is '{Status}'. " +
                "Only Late installments can be marked as missed.");
        }

        Status = InstallmentStatus.Missed;
        MarkUpdated();
    }

    public void MarkReminderSent()
    {
        ReminderSent = true;
        MarkUpdated();
    }

    public void MarkLateNoticeSent()
    {
        LateNoticeSent = true;
        MarkUpdated();
    }
}
