using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Domain.Entities;
using LoanSuperMarket.Domain.Enums;
using LoanSuperMarket.Shared.Common;
using MediatR;

namespace LoanSuperMarket.Application.Features.Users.Commands.ChangeAccountStatus;

public sealed class ChangeAccountStatusCommandHandler
    : IRequestHandler<ChangeAccountStatusCommand, ApiResponse<string>>
{
    private readonly IIdentityService _identityService;
    private readonly ITokenService _tokenService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IEmailService _emailService;
    private readonly IAuditLogRepository _auditLogRepository;

    public ChangeAccountStatusCommandHandler(
        IIdentityService identityService,
        ITokenService tokenService,
        ICurrentUserService currentUserService,
        IEmailService emailService,
        IAuditLogRepository auditLogRepository)
    {
        _identityService = identityService;
        _tokenService = tokenService;
        _currentUserService = currentUserService;
        _emailService = emailService;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<ApiResponse<string>> Handle(
        ChangeAccountStatusCommand request,
        CancellationToken cancellationToken)
    {
        // Validate reason is not empty
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            return ApiResponse<string>.Fail("A reason is required for status changes.");
        }

        // If setting to Blocked, require BlockedActivity
        if (request.NewStatus == AccountStatus.Blocked
            && string.IsNullOrWhiteSpace(request.BlockedActivity))
        {
            return ApiResponse<string>.Fail(
                "BlockedActivity is required when setting status to Blocked.");
        }

        // Verify user exists
        var user = await _identityService.GetUserByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            return ApiResponse<string>.Fail("User not found.");
        }

        var previousStatus = user.AccountStatus;

        // Prevent status change that would remove the last Active Admin
        if (request.NewStatus != AccountStatus.Active)
        {
            var userRoles = await _identityService.GetUserRolesAsync(
                request.UserId, cancellationToken);

            if (userRoles.Contains("Admin"))
            {
                var activeAdmins = await _identityService.GetUsersInRoleAsync(
                    "Admin", cancellationToken);

                var activeAdminCount = activeAdmins
                    .Count(a => a.AccountStatus == AccountStatus.Active && a.Id != request.UserId);

                if (activeAdminCount == 0)
                {
                    return ApiResponse<string>.Fail(
                        "Cannot change status. This is the last active Admin account.");
                }
            }
        }

        // Update user account status fields
        user.AccountStatus = request.NewStatus;
        user.AccountStatusReason = request.Reason;
        user.AccountStatusChangedAtUtc = DateTime.UtcNow;
        user.AccountStatusChangedBy = _currentUserService.UserId ?? "System";

        if (request.NewStatus == AccountStatus.Blocked)
        {
            user.BlockedActivity = request.BlockedActivity;
        }

        // Persist the user changes
        await _identityService.SaveUserAsync(user, cancellationToken);

        // Revoke all tokens if Suspended or Closed
        if (request.NewStatus is AccountStatus.Suspended or AccountStatus.Closed)
        {
            await _tokenService.RevokeAllUserTokensAsync(
                request.UserId,
                $"Account status changed to {request.NewStatus}",
                cancellationToken);
        }

        // Send email notification
        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            await _emailService.SendAccountStatusChangedAsync(
                user.Email,
                user.FullName,
                previousStatus,
                request.NewStatus,
                request.Reason,
                cancellationToken);
        }

        // Record audit log with previous/new status
        var performedBy = _currentUserService.UserId ?? "System";
        await _auditLogRepository.AddAsync(
            AuditLog.Create(
                "ApplicationUser",
                null,
                "AccountStatusChanged",
                $"User '{user.Email}' status changed from {previousStatus} to {request.NewStatus}. Reason: {request.Reason}.",
                performedBy),
            cancellationToken);

        await _auditLogRepository.SaveChangesAsync(cancellationToken);

        return ApiResponse<string>.Ok(
            request.UserId,
            $"Account status changed to {request.NewStatus}.");
    }
}
