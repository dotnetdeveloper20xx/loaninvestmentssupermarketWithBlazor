using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Domain.Entities;
using LoanSuperMarket.Shared.Common;
using MediatR;

namespace LoanSuperMarket.Application.Features.Auth.Commands.TwoFactor;

/// <summary>
/// Handles disabling two-factor authentication for the current user
/// and records an audit log entry for the event.
/// </summary>
public sealed class Disable2FaCommandHandler
    : IRequestHandler<Disable2FaCommand, ApiResponse<string>>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly ITwoFactorService _twoFactorService;
    private readonly IAuditLogRepository _auditLogRepository;

    public Disable2FaCommandHandler(
        ICurrentUserService currentUserService,
        ITwoFactorService twoFactorService,
        IAuditLogRepository auditLogRepository)
    {
        _currentUserService = currentUserService;
        _twoFactorService = twoFactorService;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<ApiResponse<string>> Handle(
        Disable2FaCommand request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;

        if (string.IsNullOrWhiteSpace(userId))
        {
            return ApiResponse<string>.Fail("User is not authenticated.");
        }

        // Disable 2FA for the user
        await _twoFactorService.DisableAsync(userId, cancellationToken);

        // Record audit log for 2FA disable
        var auditLog = AuditLog.Create(
            "ApplicationUser",
            entityId: null,
            "TwoFactorDisabled",
            $"Two-factor authentication disabled for user.",
            performedBy: userId);

        await _auditLogRepository.AddAsync(auditLog, cancellationToken);

        return ApiResponse<string>.Ok(
            "Two-factor authentication has been disabled.",
            "Two-factor authentication disabled successfully.");
    }
}
