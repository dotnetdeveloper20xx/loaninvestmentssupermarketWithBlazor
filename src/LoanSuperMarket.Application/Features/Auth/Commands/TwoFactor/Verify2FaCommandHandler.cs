using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Domain.Entities;
using LoanSuperMarket.Shared.Common;
using MediatR;

namespace LoanSuperMarket.Application.Features.Auth.Commands.TwoFactor;

/// <summary>
/// Handles 2FA verification: verifies the TOTP code, enables 2FA for the user,
/// generates recovery codes, and records an audit log entry.
/// </summary>
public sealed class Verify2FaCommandHandler
    : IRequestHandler<Verify2FaCommand, ApiResponse<IReadOnlyList<string>>>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly ITwoFactorService _twoFactorService;
    private readonly IAuditLogRepository _auditLogRepository;

    public Verify2FaCommandHandler(
        ICurrentUserService currentUserService,
        ITwoFactorService twoFactorService,
        IAuditLogRepository auditLogRepository)
    {
        _currentUserService = currentUserService;
        _twoFactorService = twoFactorService;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<ApiResponse<IReadOnlyList<string>>> Handle(
        Verify2FaCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;

        if (string.IsNullOrWhiteSpace(userId))
        {
            return ApiResponse<IReadOnlyList<string>>.Fail("User is not authenticated.");
        }

        if (string.IsNullOrWhiteSpace(request.Code))
        {
            return ApiResponse<IReadOnlyList<string>>.Fail("Verification code is required.");
        }

        // Verify the TOTP code
        var isValid = await _twoFactorService.VerifyCodeAsync(userId, request.Code, cancellationToken);

        if (!isValid)
        {
            return ApiResponse<IReadOnlyList<string>>.Fail("Invalid verification code.");
        }

        // Enable 2FA for the user
        await _twoFactorService.EnableAsync(userId, cancellationToken);

        // Generate recovery codes
        var recoveryCodes = await _twoFactorService.GenerateRecoveryCodesAsync(userId, cancellationToken);

        // Record audit log for 2FA enable
        var auditLog = AuditLog.Create(
            "ApplicationUser",
            entityId: null,
            "TwoFactorEnabled",
            $"Two-factor authentication enabled for user.",
            performedBy: userId);

        await _auditLogRepository.AddAsync(auditLog, cancellationToken);

        return ApiResponse<IReadOnlyList<string>>.Ok(
            recoveryCodes,
            "Two-factor authentication has been enabled. Store your recovery codes in a safe place.");
    }
}
