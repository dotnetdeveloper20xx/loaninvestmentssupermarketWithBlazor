# 21 — Late Payment & Default Detection

## Feature Requirements

The late payment system automatically detects overdue payments, applies fees, and identifies defaulted loans. Key requirements:

1. **Background Processing**: Runs daily via `IHostedService`
2. **Grace Period**: Installments are only marked late after a configurable grace period
3. **Late Fees**: A percentage-based fee is applied to overdue amounts
4. **Missed Detection**: Late installments become "Missed" when the next due date arrives
5. **Default Detection**: 3+ consecutive late/missed installments = loan default
6. **Notifications**: Upcoming reminders, late notices, and default alerts
7. **Graceful Shutdown**: Timer-based service with proper disposal

## Technologies & Patterns

| Technology | Purpose |
|---|---|
| `IHostedService` | Background service running on a timer |
| `IServiceScopeFactory` | Creates scoped services from singleton context |
| Options Pattern | `RepaymentSettings` configuration via `IOptions<T>` |
| Domain State Machine | Installment status transitions |
| Notification Service | Sends alerts via `INotificationService` |

---

## Configuration: `RepaymentSettings`

```csharp
namespace LoanSuperMarket.Shared.Configuration;

/// <summary>
/// Configuration settings for the repayment engine including grace periods,
/// late fee percentages, and notification thresholds.
/// </summary>
public sealed class RepaymentSettings
{
    /// <summary>
    /// Number of days after due date before an installment is marked late.
    /// </summary>
    public int GracePeriodDays { get; set; } = 5;

    /// <summary>
    /// Percentage of outstanding amount charged as a late fee (e.g. 0.02 = 2%).
    /// </summary>
    public decimal LateFeePercentage { get; set; } = 0.02m;

    /// <summary>
    /// Number of consecutive missed/late installments before a loan is considered defaulted.
    /// </summary>
    public int ConsecutiveMissedForDefault { get; set; } = 3;

    /// <summary>
    /// Number of days before due date to send an upcoming payment reminder.
    /// </summary>
    public int UpcomingPaymentReminderDays { get; set; } = 3;
}
```

### Configuration in `appsettings.json`

```json
{
  "RepaymentSettings": {
    "GracePeriodDays": 5,
    "LateFeePercentage": 0.02,
    "ConsecutiveMissedForDefault": 3,
    "UpcomingPaymentReminderDays": 3
  }
}
```

---

## Infrastructure: `LatePaymentHostedService`

```csharp
using LoanSuperMarket.Application.Features.Payments.LateDetection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LoanSuperMarket.Infrastructure.Services;

/// <summary>
/// Background service that runs daily to detect late payments,
/// process missed installments, detect defaults, and send reminders.
/// </summary>
public sealed class LatePaymentHostedService : IHostedService, IDisposable
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<LatePaymentHostedService> _logger;
    private Timer? _timer;

    public LatePaymentHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<LatePaymentHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Late Payment Hosted Service starting.");

        // Run once per day
        _timer = new Timer(
            DoWork,
            null,
            TimeSpan.FromMinutes(1),   // Initial delay (1 min after startup)
            TimeSpan.FromHours(24));    // Repeat every 24 hours

        return Task.CompletedTask;
    }

    private async void DoWork(object? state)
    {
        _logger.LogInformation("Late Payment Hosted Service executing daily check.");

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var latePaymentService = scope.ServiceProvider
                .GetRequiredService<LatePaymentService>();

            // Execute in order: reminders → overdue → missed → defaults
            await latePaymentService.SendUpcomingRemindersAsync(CancellationToken.None);
            await latePaymentService.ProcessOverdueInstallmentsAsync(CancellationToken.None);
            await latePaymentService.ProcessMissedInstallmentsAsync(CancellationToken.None);
            await latePaymentService.DetectDefaultsAsync(CancellationToken.None);

            _logger.LogInformation("Late Payment Hosted Service daily check completed.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Late Payment Hosted Service daily check.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Late Payment Hosted Service stopping.");
        _timer?.Change(Timeout.Infinite, 0); // Stop the timer
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}
```

### Key Design Decisions

1. **`IServiceScopeFactory`** — The hosted service is a singleton, but `LatePaymentService` needs scoped dependencies (DbContext, repositories). Creating a scope per execution solves this.

2. **`async void DoWork`** — Timer callbacks must be `void`. The `try/catch` ensures exceptions don't crash the host.

3. **Graceful Shutdown** — `StopAsync` disables the timer by setting interval to `Timeout.Infinite`. `Dispose` cleans up the timer resource.

4. **Execution Order** — Reminders first (non-destructive), then overdue marking, then missed transitions, then default detection. This ensures each step sees the correct state.

---

## Application Service: `LatePaymentService`

```csharp
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
    /// Marks installments as Late when past due + grace period.
    /// </summary>
    public async Task ProcessOverdueInstallmentsAsync(CancellationToken ct)
    {
        var allSchedules = await GetAllActiveSchedulesAsync(ct);

        foreach (var schedule in allSchedules)
        {
            foreach (var installment in schedule.Installments)
            {
                try
                {
                    if (installment.Status is not (InstallmentStatus.Pending
                        or InstallmentStatus.PartiallyPaid))
                        continue;

                    var overdueDate = installment.DueDate.AddDays(_settings.GracePeriodDays);
                    if (DateTime.UtcNow <= overdueDate)
                        continue;

                    // Mark late and apply fee
                    installment.MarkLate(_settings.LateFeePercentage);

                    // Send late notice (once)
                    if (!installment.LateNoticeSent)
                    {
                        await _notificationService.SendLatePaymentNoticeAsync(
                            schedule.Id, installment.InstallmentNumber,
                            installment.TotalAmount, installment.LateFeeAmount, ct);
                        installment.MarkLateNoticeSent();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing overdue installment #{Number}",
                        installment.InstallmentNumber);
                }
            }
            schedule.UpdatePerformance();
        }
        await _repository.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Transitions Late → Missed when next installment's due date arrives.
    /// </summary>
    public async Task ProcessMissedInstallmentsAsync(CancellationToken ct)
    {
        var allSchedules = await GetAllActiveSchedulesAsync(ct);

        foreach (var schedule in allSchedules)
        {
            var ordered = schedule.Installments.OrderBy(i => i.InstallmentNumber).ToList();

            for (var i = 0; i < ordered.Count - 1; i++)
            {
                try
                {
                    var current = ordered[i];
                    var next = ordered[i + 1];

                    if (current.Status == InstallmentStatus.Late && DateTime.UtcNow >= next.DueDate)
                        current.MarkMissed();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing missed installment");
                }
            }
            schedule.UpdatePerformance();
        }
        await _repository.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Detects 3+ consecutive Late/Missed → marks as Defaulted.
    /// </summary>
    public async Task DetectDefaultsAsync(CancellationToken ct)
    {
        var allSchedules = await GetAllActiveSchedulesAsync(ct);

        foreach (var schedule in allSchedules)
        {
            if (schedule.Performance == LoanPerformance.Defaulted)
                continue;

            try
            {
                var ordered = schedule.Installments.OrderBy(i => i.InstallmentNumber).ToList();
                var consecutiveBad = 0;
                var maxConsecutive = 0;

                foreach (var installment in ordered)
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
                        schedule.Id, schedule.LenderId,
                        schedule.LoanApplicationId, ct);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error detecting defaults for schedule {Id}", schedule.Id);
            }
        }
        await _repository.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Sends reminders for installments due within the reminder window.
    /// </summary>
    public async Task SendUpcomingRemindersAsync(CancellationToken ct)
    {
        var allSchedules = await GetAllActiveSchedulesAsync(ct);

        foreach (var schedule in allSchedules)
        {
            foreach (var installment in schedule.Installments)
            {
                try
                {
                    if (installment.Status != InstallmentStatus.Pending) continue;
                    if (installment.ReminderSent) continue;

                    var reminderDate = installment.DueDate
                        .AddDays(-_settings.UpcomingPaymentReminderDays);

                    if (DateTime.UtcNow < reminderDate || DateTime.UtcNow > installment.DueDate)
                        continue;

                    await _notificationService.SendPaymentReminderAsync(
                        schedule.Id, installment.InstallmentNumber,
                        installment.TotalAmount, installment.DueDate, ct);

                    installment.MarkReminderSent();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending reminder");
                }
            }
        }
        await _repository.SaveChangesAsync(ct);
    }

    private async Task<IReadOnlyList<RepaymentSchedule>> GetAllActiveSchedulesAsync(CancellationToken ct)
    {
        return await _repository.GetAllActiveSchedulesAsync(ct);
    }
}
```

---

## Installment Status Transitions (Late Payment Context)

```csharp
public void MarkLate(decimal lateFeePercentage)
{
    if (Status != InstallmentStatus.Pending && Status != InstallmentStatus.PartiallyPaid)
        throw new DomainException("Only Pending or PartiallyPaid can be marked late.");

    Status = InstallmentStatus.Late;
    LateFeeAmount = decimal.Round((TotalAmount - PaidAmount) * lateFeePercentage, 2);
    MarkUpdated();
}

public void MarkMissed()
{
    if (Status != InstallmentStatus.Late)
        throw new DomainException("Only Late installments can be marked as missed.");

    Status = InstallmentStatus.Missed;
    MarkUpdated();
}
```

### Late Fee Calculation

```
LateFee = (TotalAmount - PaidAmount) × LateFeePercentage
```

Example: Installment of £470.73, partially paid £200, fee rate 2%:
```
LateFee = (470.73 - 200) × 0.02 = £5.41
```

---

## Notification Triggers

| Event | Notification | Recipient |
|---|---|---|
| Installment due in 3 days | Payment reminder | Borrower |
| Installment marked late | Late payment notice | Borrower |
| Loan defaulted (3+ consecutive) | Default notice | Lender + Borrower |

---

## Graceful Shutdown Flow

```
1. Application receives shutdown signal (SIGTERM/Ctrl+C)
2. Host calls StopAsync() on all IHostedService implementations
3. LatePaymentHostedService.StopAsync():
   - Logs "stopping"
   - Disables timer: _timer.Change(Timeout.Infinite, 0)
   - Returns Task.CompletedTask
4. If DoWork is currently executing, it completes naturally
5. Host calls Dispose() → timer is disposed
```

---

## Step-by-Step Guide: Adding Escalation Levels

To add escalation (Warning → Final Notice → Collections):

1. **Configuration** — Add to `RepaymentSettings`:
```csharp
public int DaysBeforeFinalNotice { get; set; } = 14;
public int DaysBeforeCollections { get; set; } = 30;
```

2. **Domain** — Add `EscalationLevel` enum and property to `Installment`

3. **Service** — Add `ProcessEscalationsAsync()` method to `LatePaymentService`

4. **Hosted Service** — Add call after `ProcessOverdueInstallmentsAsync()`

5. **Notifications** — Add escalation-specific notification templates
