using LoanSuperMarket.Shared.Common;
using LoanSuperMarket.Shared.Configuration;
using MediatR;

namespace LoanSuperMarket.Application.Features.Notifications.GetNotificationPreferences;

public sealed record GetNotificationPreferencesQuery(string UserId)
    : IRequest<ApiResponse<NotificationPreferencesDto>>;
