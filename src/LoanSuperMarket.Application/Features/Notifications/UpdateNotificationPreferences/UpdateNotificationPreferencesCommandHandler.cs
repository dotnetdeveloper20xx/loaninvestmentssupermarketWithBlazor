using LoanSuperMarket.Shared.Common;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LoanSuperMarket.Application.Features.Notifications.UpdateNotificationPreferences;

/// <summary>
/// Saves notification preferences for a user.
/// Currently logs the update — in production this would persist to a preferences table.
/// </summary>
public sealed class UpdateNotificationPreferencesCommandHandler
    : IRequestHandler<UpdateNotificationPreferencesCommand, ApiResponse<string>>
{
    private readonly ILogger<UpdateNotificationPreferencesCommandHandler> _logger;

    public UpdateNotificationPreferencesCommandHandler(
        ILogger<UpdateNotificationPreferencesCommandHandler> logger)
    {
        _logger = logger;
    }

    public Task<ApiResponse<string>> Handle(
        UpdateNotificationPreferencesCommand request,
        CancellationToken cancellationToken)
    {
        // In production: persist to NotificationPreferences table
        _logger.LogInformation(
            "Notification preferences updated for user {UserId}: " +
            "Reminders={Reminders}, LateNotices={Late}, Defaults={Defaults}, " +
            "Funding={Funding}, Portfolio={Portfolio}, Channel={Channel}",
            request.UserId,
            request.Preferences.PaymentReminders,
            request.Preferences.LatePaymentNotices,
            request.Preferences.DefaultNotices,
            request.Preferences.FundingConfirmations,
            request.Preferences.PortfolioSummary,
            request.Preferences.PreferredChannel);

        return Task.FromResult(
            ApiResponse<string>.Ok("Preferences saved.", "Notification preferences updated."));
    }
}
