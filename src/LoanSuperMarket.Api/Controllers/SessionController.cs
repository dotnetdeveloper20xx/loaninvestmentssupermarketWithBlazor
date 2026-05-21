using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Application.Features.Sessions.Commands.RevokeSession;
using LoanSuperMarket.Application.Features.Sessions.Queries.GetUserSessions;
using LoanSuperMarket.Application.Features.Users.Models;
using LoanSuperMarket.Shared.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LoanSuperMarket.Api.Controllers;

[ApiController]
[Route("api/sessions")]
[Authorize]
public sealed class SessionController : ControllerBase
{
    private readonly ISender _sender;
    private readonly ICurrentUserService _currentUser;

    public SessionController(ISender sender, ICurrentUserService currentUser)
    {
        _sender = sender;
        _currentUser = currentUser;
    }

    [HttpGet("my")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<UserSessionDto>>>> GetMySessions(
        CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("User is not authenticated.");

        var sessions = await _sender.Send(new GetUserSessionsQuery(userId), cancellationToken);

        return Ok(ApiResponse<IReadOnlyList<UserSessionDto>>.Ok(
            sessions,
            "Sessions retrieved successfully."));
    }

    [HttpPost("{id:guid}/revoke")]
    public async Task<ActionResult<ApiResponse<string>>> RevokeSession(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new RevokeSessionCommand(id), cancellationToken);

        return Ok(result);
    }
}
