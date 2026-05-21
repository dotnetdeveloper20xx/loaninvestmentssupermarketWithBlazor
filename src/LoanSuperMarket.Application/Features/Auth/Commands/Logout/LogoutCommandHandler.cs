using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Shared.Common;
using MediatR;

namespace LoanSuperMarket.Application.Features.Auth.Commands.Logout;

/// <summary>
/// Handles logout requests by revoking the refresh token via ITokenService.
/// The token service revokes the token and terminates the associated session.
/// </summary>
public sealed class LogoutCommandHandler
    : IRequestHandler<LogoutCommand, ApiResponse<string>>
{
    private readonly ITokenService _tokenService;

    public LogoutCommandHandler(ITokenService tokenService)
    {
        _tokenService = tokenService;
    }

    public async Task<ApiResponse<string>> Handle(
        LogoutCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _tokenService.RevokeTokenAsync(
                request.RefreshToken,
                "User logout",
                cancellationToken);

            return ApiResponse<string>.Ok("Logged out successfully.");
        }
        catch (UnauthorizedAccessException ex)
        {
            return ApiResponse<string>.Fail(ex.Message);
        }
    }
}
