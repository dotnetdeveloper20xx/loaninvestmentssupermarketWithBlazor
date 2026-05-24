using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Application.Features.Notifications.GetNotificationPreferences;
using LoanSuperMarket.Application.Features.Notifications.UpdateNotificationPreferences;
using LoanSuperMarket.Shared.Common;
using LoanSuperMarket.Shared.Configuration;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LoanSuperMarket.Api.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public sealed class NotificationsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ICurrentUserService _currentUserService;

    public NotificationsController(ISender sender, ICurrentUserService currentUserService)
    {
        _sender = sender;
        _currentUserService = currentUserService;
    }

    [HttpGet("preferences")]
    public async Task<ActionResult<ApiResponse<NotificationPreferencesDto>>> GetPreferences(
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId ?? string.Empty;
        var result = await _sender.Send(
            new GetNotificationPreferencesQuery(userId), cancellationToken);
        return Ok(result);
    }

    [HttpPut("preferences")]
    public async Task<ActionResult<ApiResponse<string>>> UpdatePreferences(
        [FromBody] NotificationPreferencesDto preferences,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId ?? string.Empty;
        var command = new UpdateNotificationPreferencesCommand(userId, preferences);
        var result = await _sender.Send(command, cancellationToken);
        return Ok(result);
    }
}
