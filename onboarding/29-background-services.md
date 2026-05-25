# 29 — Background Services

## Overview

The platform uses ASP.NET Core's `IHostedService` pattern to run background tasks. The primary example is `LatePaymentHostedService`, which runs daily to detect overdue installments, process missed payments, detect loan defaults, and send payment reminders. This document covers the hosted service pattern, timer-based execution, scoped service resolution, graceful shutdown, and error handling.

---

## Feature Requirements (Plain English)

1. Run a daily check for late/overdue loan payments.
2. Send reminders for upcoming payments (due within 3 days).
3. Mark installments as overdue when past due date.
4. Mark installments as missed after a grace period.
5. Detect loan defaults (3+ consecutive missed payments).
6. Run automatically when the API starts, without manual triggering.
7. Handle errors gracefully without crashing the host.
8. Support graceful shutdown when the application stops.

---

## Technologies & Patterns

| Concern | Technology | Pattern |
|---------|-----------|---------|
| Background execution | IHostedService | Timer-based periodic work |
| Scoped services | IServiceScopeFactory | Manual scope creation |
| Configuration | IOptions<T> | Strongly-typed settings |
| Logging | ILogger<T> | Structured logging |

---

## The IHostedService Pattern

ASP.NET Core's `IHostedService` interface defines two methods:
- `StartAsync` — Called when the application starts.
- `StopAsync` — Called when the application is shutting down.

For periodic work, you combine this with a `Timer`.

---

## LatePaymentHostedService — Full Implementation

```csharp
// src/LoanSuperMarket.Infrastructure/Services/LatePaymentHostedService.cs
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
            TimeSpan.FromMinutes(1),   // Initial delay (let app fully start)
            TimeSpan.FromHours(24));   // Repeat every 24 hours

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
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}
```

---

## Why IServiceScopeFactory?

Hosted services are registered as **singletons** (they live for the entire application lifetime). But most services (DbContext, repositories) are **scoped** (one instance per request). You can't inject scoped services into a singleton.

The solution: Create a new scope manually for each execution:

```csharp
using var scope = _scopeFactory.CreateScope();
var service = scope.ServiceProvider.GetRequiredService<LatePaymentService>();
```

When the `using` block ends, the scope is disposed, which disposes all scoped services (including the DbContext).

---

## Timer-Based Periodic Execution

```csharp
_timer = new Timer(
    callback: DoWork,       // Method to call
    state: null,            // State object (unused)
    dueTime: TimeSpan.FromMinutes(1),  // Wait 1 minute before first execution
    period: TimeSpan.FromHours(24)     // Then repeat every 24 hours
);
```

### Why the initial delay?

The 1-minute delay ensures the application is fully started (database migrations applied, seed data loaded) before the background service begins its work.

### Why `async void`?

The `Timer` callback signature requires `void`. Since we need to do async work, we use `async void`. This is one of the few acceptable uses of `async void` — the key is to **catch all exceptions** inside the method:

```csharp
private async void DoWork(object? state)
{
    try
    {
        // async work here
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error in background service.");
        // NEVER let exceptions escape async void — it crashes the process
    }
}
```

---

## Graceful Shutdown

```csharp
public Task StopAsync(CancellationToken cancellationToken)
{
    _logger.LogInformation("Late Payment Hosted Service stopping.");

    // Disable the timer (don't fire again)
    _timer?.Change(Timeout.Infinite, 0);

    return Task.CompletedTask;
}
```

`Change(Timeout.Infinite, 0)` tells the timer to never fire again. If `DoWork` is currently executing, it will complete naturally (we don't interrupt it).

For long-running operations, you could pass the `cancellationToken` to the work method:

```csharp
public Task StopAsync(CancellationToken cancellationToken)
{
    _cts?.Cancel(); // Signal the work to stop
    _timer?.Change(Timeout.Infinite, 0);
    return Task.CompletedTask;
}
```

---

## DI Registration

```csharp
// src/LoanSuperMarket.Infrastructure/DependencyInjection.cs
services.AddHostedService<LatePaymentHostedService>();
```

`AddHostedService<T>()` registers the service as a singleton and tells the host to call `StartAsync`/`StopAsync` during application lifecycle.

---

## Configuration via IOptions

```csharp
// src/LoanSuperMarket.Shared/Configuration/RepaymentSettings.cs
namespace LoanSuperMarket.Shared.Configuration;

public sealed class RepaymentSettings
{
    public const string SectionName = "RepaymentSettings";

    public int GracePeriodDays { get; init; } = 7;
    public int DefaultThresholdMissedPayments { get; init; } = 3;
    public int ReminderDaysBeforeDue { get; init; } = 3;
    public decimal LateFeePercentage { get; init; } = 2.0m;
}
```

```json
// appsettings.json
{
  "RepaymentSettings": {
    "GracePeriodDays": 7,
    "DefaultThresholdMissedPayments": 3,
    "ReminderDaysBeforeDue": 3,
    "LateFeePercentage": 2.0
  }
}
```

```csharp
// DependencyInjection.cs
services.Configure<RepaymentSettings>(
    configuration.GetSection("RepaymentSettings"));
```

The `LatePaymentService` receives `IOptions<RepaymentSettings>` to use these values:

```csharp
public sealed class LatePaymentService
{
    private readonly RepaymentSettings _settings;

    public LatePaymentService(IOptions<RepaymentSettings> options, ...)
    {
        _settings = options.Value;
    }

    public async Task ProcessOverdueInstallmentsAsync(CancellationToken ct)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-_settings.GracePeriodDays);
        // Query installments due before cutoffDate that aren't paid...
    }
}
```

---

## Error Handling in Background Services

### Rule 1: Never let exceptions escape

```csharp
private async void DoWork(object? state)
{
    try
    {
        // All work inside try block
    }
    catch (Exception ex)
    {
        // Log and swallow — the service continues running
        _logger.LogError(ex, "Background service error.");
    }
}
```

### Rule 2: Log with context

```csharp
_logger.LogError(ex,
    "Error processing overdue installments. " +
    "Affected count: {Count}, Cutoff: {Cutoff}",
    overdueCount, cutoffDate);
```

### Rule 3: Consider retry logic

For transient failures (database timeouts), you might add a simple retry:

```csharp
private async Task ExecuteWithRetryAsync(Func<Task> action, int maxRetries = 3)
{
    for (int i = 0; i < maxRetries; i++)
    {
        try
        {
            await action();
            return;
        }
        catch (Exception ex) when (i < maxRetries - 1)
        {
            _logger.LogWarning(ex, "Retry {Attempt}/{Max}", i + 1, maxRetries);
            await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, i))); // Exponential backoff
        }
    }
}
```

---

## Alternative: BackgroundService Base Class

For simpler cases, you can inherit from `BackgroundService` instead of implementing `IHostedService` directly:

```csharp
public sealed class SimpleBackgroundService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                // Do work...
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in background service.");
            }

            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }
}
```

The `BackgroundService` approach is simpler but less flexible than the Timer approach (e.g., you can't easily change the interval at runtime).

---

## Step-by-Step Extension Guide

### Adding a new background service (e.g., "Daily Report Generator")

1. **Create the service class:**
   ```csharp
   public sealed class DailyReportHostedService : IHostedService, IDisposable
   {
       private Timer? _timer;
       private readonly IServiceScopeFactory _scopeFactory;
       private readonly ILogger<DailyReportHostedService> _logger;

       public Task StartAsync(CancellationToken ct)
       {
           _timer = new Timer(DoWork, null,
               TimeSpan.FromHours(1),  // First run after 1 hour
               TimeSpan.FromHours(24)); // Then daily
           return Task.CompletedTask;
       }

       private async void DoWork(object? state)
       {
           try
           {
               using var scope = _scopeFactory.CreateScope();
               var reportService = scope.ServiceProvider
                   .GetRequiredService<IReportGeneratorService>();
               await reportService.GenerateDailyReportAsync(CancellationToken.None);
           }
           catch (Exception ex)
           {
               _logger.LogError(ex, "Daily report generation failed.");
           }
       }

       public Task StopAsync(CancellationToken ct)
       {
           _timer?.Change(Timeout.Infinite, 0);
           return Task.CompletedTask;
       }

       public void Dispose() => _timer?.Dispose();
   }
   ```

2. **Register in DI:**
   ```csharp
   services.AddHostedService<DailyReportHostedService>();
   ```

3. **Add configuration (optional):**
   ```json
   { "ReportSettings": { "RunAtHourUtc": 2, "Recipients": ["admin@example.com"] } }
   ```

---

## Testing Considerations

- **Unit test the work method:** Extract the logic into a separate service class (like `LatePaymentService`) that can be tested independently.
- **Integration test:** Use `WebApplicationFactory` and verify the hosted service starts.
- **Time-sensitive tests:** Use `ISystemClock` abstraction to control time in tests.

---

## Common Pitfalls

1. **Injecting scoped services directly** — Will throw at runtime. Always use `IServiceScopeFactory`.
2. **Unhandled exceptions in `async void`** — Crashes the entire process. Always wrap in try/catch.
3. **Timer drift** — `Timer` doesn't guarantee exact intervals. For precise scheduling, consider Hangfire or Quartz.NET.
4. **Multiple instances** — In a load-balanced environment, multiple API instances will each run the hosted service. Use a distributed lock (Redis, database) to prevent duplicate execution.
5. **Long-running work blocking shutdown** — If `DoWork` takes 30 minutes, `StopAsync` returns immediately but the work continues. Consider using `CancellationToken` for interruptible work.
