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
            TimeSpan.FromMinutes(1), // Initial delay
            TimeSpan.FromHours(24)); // Repeat every 24 hours

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
