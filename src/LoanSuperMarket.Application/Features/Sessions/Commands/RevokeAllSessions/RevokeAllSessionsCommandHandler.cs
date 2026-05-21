using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Shared.Common;
using MediatR;

namespace LoanSuperMarket.Application.Features.Sessions.Commands.RevokeAllSessions;

/// <summary>
/// Handles revoking all sessions for a user by delegating to ISessionService.
/// The session service invalidates all sessions and their associated refresh tokens.
/// </summary>
public sealed class RevokeAllSessionsCommandHandler
    : IRequestHandler<RevokeAllSessionsCommand, ApiResponse<string>>
{
    private readonly ISessionService _sessionService;
    private readonly ICurrentUserService _currentUserService;

    public RevokeAllSessionsCommandHandler(
        ISessionService sessionService,
        ICurrentUserService currentUserService)
    {
        _sessionService = sessionService;
        _currentUserService = currentUserService;
    }

    public async Task<ApiResponse<string>> Handle(
        RevokeAllSessionsCommand request,
        CancellationToken cancellationToken)
    {
        var userId = request.UserId ?? _currentUserService.UserId;

        if (string.IsNullOrEmpty(userId))
        {
            return ApiResponse<string>.Fail("User is not authenticated.");
        }

        try
        {
            await _sessionService.RevokeAllSessionsAsync(
                userId,
                exceptSessionId: null,
                cancellationToken);

            return ApiResponse<string>.Ok("All sessions revoked successfully.");
        }
        catch (InvalidOperationException ex)
        {
            return ApiResponse<string>.Fail(ex.Message);
        }
    }
}
