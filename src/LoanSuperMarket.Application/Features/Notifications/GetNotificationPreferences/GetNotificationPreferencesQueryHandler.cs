using LoanSuperMarket.Shared.Common;
using LoanSuperMarket.Shared.Configuration;
using MediatR;

namespace LoanSuperMarket.Application.Features.Notifications.GetNotificationPreferences;

/// <summary>
/// Returns notification preferences for a user.
/// Currently returns defaults — in production this would read from a preferences table.
/// </summary>
public sealed class GetNotificationPreferencesQueryHandler
    : IRequestHandler<GetNotificationPreferencesQuery, ApiResponse<NotificationPreferencesDto>>
{
    public Task<ApiResponse<NotificationPreferencesDto>> Handle(
        GetNotificationPreferencesQuery request,
        CancellationToken cancellationToken)
    {
        // Default preferences — in production, load from DB per user
        var prefs = new NotificationPreferencesDto
        {
            PaymentReminders = true,
            LatePaymentNotices = true,
            DefaultNotices = true,
            FundingConfirmations = true,
            PortfolioSummary = false,
            PreferredChannel = "Email"
        };

        return Task.FromResult(
            ApiResponse<NotificationPreferencesDto>.Ok(prefs, "Preferences loaded."));
    }
}
