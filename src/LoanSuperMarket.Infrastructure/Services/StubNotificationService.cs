using LoanSuperMarket.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace LoanSuperMarket.Infrastructure.Services;

/// <summary>
/// Stub implementation of INotificationService that logs notifications
/// without sending actual emails or messages.
/// </summary>
public sealed class StubNotificationService : INotificationService
{
    private readonly ILogger<StubNotificationService> _logger;

    public StubNotificationService(ILogger<StubNotificationService> logger)
    {
        _logger = logger;
    }

    public Task SendPaymentReminderAsync(
        Guid scheduleId,
        int installmentNumber,
        decimal amount,
        DateTime dueDate,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "[NOTIFICATION] Payment reminder: Schedule {ScheduleId}, Installment #{Number}, " +
            "Amount {Amount:N2}, Due {DueDate:yyyy-MM-dd}",
            scheduleId, installmentNumber, amount, dueDate);

        return Task.CompletedTask;
    }

    public Task SendLatePaymentNoticeAsync(
        Guid scheduleId,
        int installmentNumber,
        decimal amount,
        decimal lateFee,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning(
            "[NOTIFICATION] Late payment notice: Schedule {ScheduleId}, Installment #{Number}, " +
            "Amount {Amount:N2}, Late Fee {LateFee:N2}",
            scheduleId, installmentNumber, amount, lateFee);

        return Task.CompletedTask;
    }

    public Task SendDefaultNoticeAsync(
        Guid scheduleId,
        Guid lenderId,
        Guid loanApplicationId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogError(
            "[NOTIFICATION] Default notice: Schedule {ScheduleId}, Lender {LenderId}, " +
            "Application {ApplicationId}",
            scheduleId, lenderId, loanApplicationId);

        return Task.CompletedTask;
    }
}
