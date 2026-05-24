namespace LoanSuperMarket.Application.Common.Interfaces;

/// <summary>
/// Pushes real-time notifications to connected clients via SignalR.
/// </summary>
public interface IRealTimeNotifier
{
    /// <summary>Notifies all lenders that the funding queue has changed.</summary>
    Task NotifyFundingQueueChangedAsync(CancellationToken cancellationToken = default);

    /// <summary>Notifies a specific user that a payment was recorded on their loan.</summary>
    Task NotifyPaymentRecordedAsync(string userId, Guid scheduleId, decimal amount, CancellationToken cancellationToken = default);

    /// <summary>Notifies a specific user that their loan was funded.</summary>
    Task NotifyLoanFundedAsync(string borrowerUserId, Guid applicationId, decimal amount, CancellationToken cancellationToken = default);
}
