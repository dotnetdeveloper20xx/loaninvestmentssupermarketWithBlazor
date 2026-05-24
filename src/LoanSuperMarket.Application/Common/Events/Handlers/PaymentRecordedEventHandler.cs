using MediatR;
using Microsoft.Extensions.Logging;

namespace LoanSuperMarket.Application.Common.Events.Handlers;

/// <summary>
/// Handles the PaymentRecordedEvent by logging the payment.
/// </summary>
public sealed class PaymentRecordedEventHandler : INotificationHandler<PaymentRecordedEvent>
{
    private readonly ILogger<PaymentRecordedEventHandler> _logger;

    public PaymentRecordedEventHandler(ILogger<PaymentRecordedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(PaymentRecordedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Domain Event: Payment recorded. Schedule={ScheduleId}, Installment=#{Number}, Amount={Amount}, Status={Status}",
            notification.ScheduleId, notification.InstallmentNumber, notification.Amount, notification.Status);

        return Task.CompletedTask;
    }
}
