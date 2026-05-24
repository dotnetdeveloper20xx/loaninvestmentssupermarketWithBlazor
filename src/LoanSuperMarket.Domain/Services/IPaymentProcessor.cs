using LoanSuperMarket.Domain.Entities;

namespace LoanSuperMarket.Domain.Services;

/// <summary>
/// Domain service responsible for processing payments against a repayment schedule.
/// Enforces sequential payment order and delegates to installment state machine.
/// </summary>
public interface IPaymentProcessor
{
    /// <summary>
    /// Records a payment against the next pending installment in the schedule.
    /// </summary>
    void RecordPayment(RepaymentSchedule schedule, decimal amount, DateTime paymentDate);

    /// <summary>
    /// Records a bulk payment that pays off multiple installments sequentially.
    /// Returns the number of installments fully paid.
    /// </summary>
    int RecordBulkPayment(RepaymentSchedule schedule, decimal totalAmount, DateTime paymentDate);
}
