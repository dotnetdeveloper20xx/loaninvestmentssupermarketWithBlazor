using LoanSuperMarket.Shared.Common;
using LoanSuperMarket.Shared.Configuration;
using MediatR;

namespace LoanSuperMarket.Application.Features.Notifications.UpdateNotificationPreferences;

public sealed record UpdateNotificationPreferencesCommand(
    string UserId,
    NotificationPreferencesDto Preferences) : IRequest<ApiResponse<string>>;
