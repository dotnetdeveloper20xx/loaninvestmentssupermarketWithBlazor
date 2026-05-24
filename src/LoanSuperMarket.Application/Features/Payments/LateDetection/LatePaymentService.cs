using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Domain.Enums;
using LoanSuperMarket.Shared.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LoanSuperMarket.Application.Features.Payments.LateDetection;

/// <summary>
/// Application service that detects overdue installments, applies late fees,
/// transitions statuses, detects defaults, and sends notifications.
/// </summary>
public sealed class LatePaymentService
{
    private readonly ILoanApplicationRepository _repository;
    private readonly INotificationService _notificationService;
    private readonly RepaymentSettings _settings;
    private readonly ILogger<LatePaymentService> _logger;

    public LatePaymentService(
        ILoanApplicationRepository repository,
        INotificationService notificationService,
        IOptions<RepaymentSettings> settings,
        ILogger<LatePaymentService> logger)
    {
        _repository = repository;
        _notificationService = notificationService;
        _settings = settings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Marks installments as Late when they are past due + grace period.
    /// </summary>
    public async Task ProcessOverdueInstallmentsAsync(CancellationToken cancellationToken)
    {
        var allSchedules = await GetAllActiveSchedulesAsync(cancellationToken);

        foreach (var schedule in allSchedules)
        {
            foreach (var installment in schedule.Installments)
            {
                try
                {
                    if (installment.Status is not (InstallmentStatus.Pending or InstallmentStatus.PartiallyPaid))
                        continue;

                    var overdueDate = installment.DueDate.AddDays(_settings.GracePeriodDays);
                    if (DateTime.UtcNow <= overdueDate)
                        continue;

                    installment.MarkLate(_settings.LateFeePercentage);

                    if (!installment.LateNoticeSent)
                    {
                        await _notificationService.SendLatePaymentNoticeAsync(
                            schedule.Id,
                            installment.InstallmentNumber,
                            installment.TotalAmount,
                            installment.LateFeeAmount,
                            cancellationToken);

                        installment.MarkLateNoticeSent();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Error processing overdue installment #{Number} for schedule {ScheduleId}",
                        installment.InstallmentNumber, schedule.Id);
                }
            }

            schedule.UpdatePerformance();
        }

        await _repository.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Transitions Late installments to Missed when the next installment's due date has arrived.
    /// </summary>
    public async Task ProcessMissedInstallmentsAsync(CancellationToken cancellationToken)
    {
        var allSchedules = await GetAllActiveSchedulesAsync(cancellationToken);

        foreach (var schedule in allSchedules)
        {
            var orderedInstallments = schedule.Installments
                .OrderBy(i => i.InstallmentNumber)
                .ToList();

            for (var i = 0; i < orderedInstallments.Count - 1; i++)
            {
                try
                {
                    var current = orderedInstallments[i];
                    var next = orderedInstallments[i + 1];

                    if (current.Status == InstallmentStatus.Late && DateTime.UtcNow >= next.DueDate)
                    {
                        current.MarkMissed();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Error processing missed installment for schedule {ScheduleId}",
                        schedule.Id);
                }
            }

            schedule.UpdatePerformance();
        }

        await _repository.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Detects loans with 3+ consecutive Late/Missed installments and marks them as Defaulted.
    /// </summary>
    public async Task DetectDefaultsAsync(CancellationToken cancellationToken)
    {
        var allSchedules = await GetAllActiveSchedulesAsync(cancellationToken);

        foreach (var schedule in allSchedules)
        {
            if (schedule.Performance == LoanPerformance.Defaulted)
                continue;

            try
            {
                var orderedInstallments = schedule.Installments
                    .OrderBy(i => i.InstallmentNumber)
                    .ToList();

                var consecutiveBad = 0;
                var maxConsecutive = 0;

                foreach (var installment in orderedInstallments)
                {
                    if (installment.Status is InstallmentStatus.Late or InstallmentStatus.Missed)
                    {
                        consecutiveBad++;
                        maxConsecutive = Math.Max(maxConsecutive, consecutiveBad);
                    }
                    else
                    {
                        consecutiveBad = 0;
                    }
                }

                if (maxConsecutive >= _settings.ConsecutiveMissedForDefault)
                {
                    schedule.UpdatePerformance();

                    await _notificationService.SendDefaultNoticeAsync(
                        schedule.Id,
                        schedule.LenderId,
                        schedule.LoanApplicationId,
                        cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error detecting defaults for schedule {ScheduleId}",
                    schedule.Id);
            }
        }

        await _repository.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Sends reminders for installments due within the configured reminder window.
    /// </summary>
    public async Task SendUpcomingRemindersAsync(CancellationToken cancellationToken)
    {
        var allSchedules = await GetAllActiveSchedulesAsync(cancellationToken);

        foreach (var schedule in allSchedules)
        {
            foreach (var installment in schedule.Installments)
            {
                try
                {
                    if (installment.Status != InstallmentStatus.Pending)
                        continue;

                    if (installment.ReminderSent)
                        continue;

                    var reminderDate = installment.DueDate.AddDays(-_settings.UpcomingPaymentReminderDays);
                    if (DateTime.UtcNow < reminderDate || DateTime.UtcNow > installment.DueDate)
                        continue;

                    await _notificationService.SendPaymentReminderAsync(
                        schedule.Id,
                        installment.InstallmentNumber,
                        installment.TotalAmount,
                        installment.DueDate,
                        cancellationToken);

                    installment.MarkReminderSent();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Error sending reminder for installment #{Number} schedule {ScheduleId}",
                        installment.InstallmentNumber, schedule.Id);
                }
            }
        }

        await _repository.SaveChangesAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<Domain.Entities.RepaymentSchedule>> GetAllActiveSchedulesAsync(
        CancellationToken cancellationToken)
    {
        return await _repository.GetAllActiveSchedulesAsync(cancellationToken);
    }
}
