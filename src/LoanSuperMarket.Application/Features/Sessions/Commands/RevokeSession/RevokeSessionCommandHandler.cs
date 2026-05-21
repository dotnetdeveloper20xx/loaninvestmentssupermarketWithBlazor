using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Shared.Common;
using MediatR;

namespace LoanSuperMarket.Application.Features.Sessions.Commands.RevokeSession;

/// <summary>
/// Handles session revocation by delegating to ISessionService.
/// The session service invalidates the session and its associated refresh token.
/// </summary>
public sealed class RevokeSessionCommandHandler
    : IRequestHandler<RevokeSessionCommand, ApiResponse<string>>
{
    private readonly ISessionService _sessionService;
    private readonly ICurrentUserService _currentUserService;

    public RevokeSessionCommandHandler(
        ISessionService sessionService,
        ICurrentUserService currentUserService)
    {
        _sessionService = sessionService;
        _currentUserService = currentUserService;
    }

    public async Task<ApiResponse<string>> Handle(
        RevokeSessionCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;

        if (string.IsNullOrEmpty(userId))
        {
            return ApiResponse<string>.Fail("User is not authenticated.");
        }

        try
        {
            await _sessionService.RevokeSessionAsync(
                request.SessionId,
                userId,
                cancellationToken);

            return ApiResponse<string>.Ok("Session revoked successfully.");
        }
        catch (InvalidOperationException ex)
        {
            return ApiResponse<string>.Fail(ex.Message);
        }
    }
}
