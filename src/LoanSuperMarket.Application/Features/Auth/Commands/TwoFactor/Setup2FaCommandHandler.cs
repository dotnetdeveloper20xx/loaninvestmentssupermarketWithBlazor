using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Application.Features.Auth.Models;
using LoanSuperMarket.Shared.Common;
using MediatR;

namespace LoanSuperMarket.Application.Features.Auth.Commands.TwoFactor;

/// <summary>
/// Handles 2FA setup by generating a TOTP secret and QR code URI for the current user.
/// </summary>
public sealed class Setup2FaCommandHandler
    : IRequestHandler<Setup2FaCommand, ApiResponse<TwoFactorSetupResponse>>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly ITwoFactorService _twoFactorService;

    public Setup2FaCommandHandler(
        ICurrentUserService currentUserService,
        ITwoFactorService twoFactorService)
    {
        _currentUserService = currentUserService;
        _twoFactorService = twoFactorService;
    }

    public async Task<ApiResponse<TwoFactorSetupResponse>> Handle(
        Setup2FaCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;

        if (string.IsNullOrWhiteSpace(userId))
        {
            return ApiResponse<TwoFactorSetupResponse>.Fail("User is not authenticated.");
        }

        var setupResponse = await _twoFactorService.GenerateSetupAsync(userId, cancellationToken);

        return ApiResponse<TwoFactorSetupResponse>.Ok(
            setupResponse,
            "Two-factor authentication setup initiated. Scan the QR code with your authenticator app.");
    }
}
