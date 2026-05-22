namespace LoanSuperMarket.Application.Common.Interfaces;

/// <summary>
/// Service for sending notifications related to loan repayments.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Sends a payment reminder for an upcoming installment.
    /// </summary>
    Task SendPaymentReminderAsync(
        Guid scheduleId,
        int installmentNumber,
        decimal amount,
        DateTime dueDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a late payment notice when an installment becomes overdue.
    /// </summary>
    Task SendLatePaymentNoticeAsync(
        Guid scheduleId,
        int installmentNumber,
        decimal amount,
        decimal lateFee,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a default notice when a loan enters default status.
    /// </summary>
    Task SendDefaultNoticeAsync(
        Guid scheduleId,
        Guid lenderId,
        Guid loanApplicationId,
        CancellationToken cancellationToken = default);
}
