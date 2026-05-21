using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Domain.Entities;
using LoanSuperMarket.Shared.Common;
using MediatR;

namespace LoanSuperMarket.Application.Features.Auth.Commands.ResetPassword;

/// <summary>
/// Handles the ResetPassword command. Validates the reset token, updates the password,
/// revokes all active refresh tokens for security, and records an audit log entry.
/// </summary>
public sealed class ResetPasswordCommandHandler
    : IRequestHandler<ResetPasswordCommand, ApiResponse<string>>
{
    private readonly IIdentityService _identityService;
    private readonly ITokenService _tokenService;
    private readonly IAuditLogRepository _auditLogRepository;

    public ResetPasswordCommandHandler(
        IIdentityService identityService,
        ITokenService tokenService,
        IAuditLogRepository auditLogRepository)
    {
        _identityService = identityService;
        _tokenService = tokenService;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<ApiResponse<string>> Handle(
        ResetPasswordCommand request,
        CancellationToken cancellationToken)
    {
        // Validate token and update password via Identity
        var succeeded = await _identityService.ResetPasswordAsync(
            request.Email,
            request.Token,
            request.NewPassword,
            cancellationToken);

        if (!succeeded)
        {
            return ApiResponse<string>.Fail("Invalid or expired password reset token.");
        }

        // Retrieve user to get their ID for token revocation and audit
        var user = await _identityService.GetUserByEmailAsync(request.Email, cancellationToken);

        if (user is not null)
        {
            // Revoke all active refresh tokens for security after password reset
            await _tokenService.RevokeAllUserTokensAsync(
                user.Id,
                "Password reset",
                cancellationToken);

            // Record audit log entry for the password reset event
            await _auditLogRepository.AddAsync(
                AuditLog.Create(
                    "ApplicationUser",
                    null,
                    "PasswordReset",
                    $"Password was reset for user {request.Email}.",
                    user.Id),
                cancellationToken);
        }

        return ApiResponse<string>.Ok(
            "Password has been reset successfully.",
            "Password reset completed.");
    }
}
