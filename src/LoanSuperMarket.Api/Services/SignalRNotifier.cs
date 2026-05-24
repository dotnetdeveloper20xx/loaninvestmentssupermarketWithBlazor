using LoanSuperMarket.Api.Hubs;
using LoanSuperMarket.Application.Common.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace LoanSuperMarket.Api.Services;

/// <summary>
/// SignalR implementation of IRealTimeNotifier.
/// Pushes events to connected clients via the LoanHub.
/// </summary>
public sealed class SignalRNotifier : IRealTimeNotifier
{
    private readonly IHubContext<LoanHub> _hubContext;

    public SignalRNotifier(IHubContext<LoanHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifyFundingQueueChangedAsync(CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.Group("lenders")
            .SendAsync("FundingQueueChanged", cancellationToken);
    }

    public async Task NotifyPaymentRecordedAsync(
        string userId, Guid scheduleId, decimal amount, CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.Group($"user-{userId}")
            .SendAsync("PaymentRecorded", new { scheduleId, amount }, cancellationToken);
    }

    public async Task NotifyLoanFundedAsync(
        string borrowerUserId, Guid applicationId, decimal amount, CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.Group($"user-{borrowerUserId}")
            .SendAsync("LoanFunded", new { applicationId, amount }, cancellationToken);
    }
}
