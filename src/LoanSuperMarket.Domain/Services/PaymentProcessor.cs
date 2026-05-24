using LoanSuperMarket.Domain.Common;
using LoanSuperMarket.Domain.Entities;

namespace LoanSuperMarket.Domain.Services;

/// <summary>
/// Domain service that enforces sequential payment order and delegates
/// to the installment entity's state machine methods.
/// </summary>
public sealed class PaymentProcessor : IPaymentProcessor
{
    public void RecordPayment(RepaymentSchedule schedule, decimal amount, DateTime paymentDate)
    {
        if (amount <= 0)
        {
            throw new DomainException("Payment amount must be greater than zero.");
        }

        var nextInstallment = schedule.GetNextPendingInstallment();

        if (nextInstallment is null)
        {
            throw new DomainException("No pending installments found. All payments are complete.");
        }

        var totalOwed = nextInstallment.TotalAmount + nextInstallment.LateFeeAmount - nextInstallment.PaidAmount;

        if (amount > totalOwed)
        {
            throw new DomainException(
                $"Payment of {amount:N2} exceeds the remaining balance of {totalOwed:N2} " +
                $"on installment #{nextInstallment.InstallmentNumber}.");
        }

        if (amount >= totalOwed)
        {
            nextInstallment.RecordFullPayment(paymentDate);
        }
        else
        {
            nextInstallment.RecordPartialPayment(amount, paymentDate);
        }

        schedule.UpdatePerformance();
    }

    public int RecordBulkPayment(RepaymentSchedule schedule, decimal totalAmount, DateTime paymentDate)
    {
        if (totalAmount <= 0)
        {
            throw new DomainException("Payment amount must be greater than zero.");
        }

        var remaining = totalAmount;
        var installmentsPaid = 0;

        while (remaining > 0)
        {
            var nextInstallment = schedule.GetNextPendingInstallment();
            if (nextInstallment is null)
            {
                break; // All installments paid off
            }

            var owed = nextInstallment.TotalAmount + nextInstallment.LateFeeAmount - nextInstallment.PaidAmount;

            if (remaining >= owed)
            {
                nextInstallment.RecordFullPayment(paymentDate);
                remaining -= owed;
                installmentsPaid++;
            }
            else
            {
                nextInstallment.RecordPartialPayment(remaining, paymentDate);
                remaining = 0;
            }
        }

        schedule.UpdatePerformance();
        return installmentsPaid;
    }
}
