using LoanSuperMarket.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LoanSuperMarket.Application.Common.Events.Handlers;

/// <summary>
/// Handles the LoanFundedEvent by sending real-time notifications
/// and logging the event.
/// </summary>
public sealed class LoanFundedEventHandler : INotificationHandler<LoanFundedEvent>
{
    private readonly IRealTimeNotifier _notifier;
    private readonly ILogger<LoanFundedEventHandler> _logger;

    public LoanFundedEventHandler(IRealTimeNotifier notifier, ILogger<LoanFundedEventHandler> logger)
    {
        _notifier = notifier;
        _logger = logger;
    }

    public async Task Handle(LoanFundedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Domain Event: Loan funded. Application={AppId}, Lender={LenderId}, Amount={Amount}",
            notification.ApplicationId, notification.LenderId, notification.Amount);

        await _notifier.NotifyFundingQueueChangedAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(notification.BorrowerUserId))
        {
            await _notifier.NotifyLoanFundedAsync(
                notification.BorrowerUserId,
                notification.ApplicationId,
                notification.Amount,
                cancellationToken);
        }
    }
}
