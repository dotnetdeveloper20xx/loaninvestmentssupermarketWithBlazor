using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Application.Features.Auth.Models;
using LoanSuperMarket.Shared.Common;
using MediatR;

namespace LoanSuperMarket.Application.Features.Auth.Commands.RefreshToken;

/// <summary>
/// Handles refresh token requests by delegating to ITokenService.
/// The token service handles rotation (revoking old token, issuing new pair)
/// and reuse detection (revoking all user tokens if a revoked token is presented).
/// </summary>
public sealed class RefreshTokenCommandHandler
    : IRequestHandler<RefreshTokenCommand, ApiResponse<AuthTokenResponse>>
{
    private readonly ITokenService _tokenService;

    public RefreshTokenCommandHandler(ITokenService tokenService)
    {
        _tokenService = tokenService;
    }

    public async Task<ApiResponse<AuthTokenResponse>> Handle(
        RefreshTokenCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var tokenResponse = await _tokenService.RefreshTokenAsync(
                request.RefreshToken,
                cancellationToken);

            return ApiResponse<AuthTokenResponse>.Ok(tokenResponse, "Token refreshed successfully.");
        }
        catch (UnauthorizedAccessException ex)
        {
            return ApiResponse<AuthTokenResponse>.Fail(ex.Message);
        }
    }
}
