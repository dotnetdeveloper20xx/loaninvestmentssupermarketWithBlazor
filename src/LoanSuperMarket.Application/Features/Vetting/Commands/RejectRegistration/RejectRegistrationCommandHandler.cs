using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Domain.Entities;
using LoanSuperMarket.Domain.Enums;
using LoanSuperMarket.Shared.Common;
using MediatR;

namespace LoanSuperMarket.Application.Features.Vetting.Commands.RejectRegistration;

public sealed class RejectRegistrationCommandHandler
    : IRequestHandler<RejectRegistrationCommand, ApiResponse<string>>
{
    private readonly IIdentityService _identityService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IEmailService _emailService;
    private readonly IAuditLogRepository _auditLogRepository;

    public RejectRegistrationCommandHandler(
        IIdentityService identityService,
        ICurrentUserService currentUserService,
        IEmailService emailService,
        IAuditLogRepository auditLogRepository)
    {
        _identityService = identityService;
        _currentUserService = currentUserService;
        _emailService = emailService;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<ApiResponse<string>> Handle(
        RejectRegistrationCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            return ApiResponse<string>.Fail("A reason is required for rejection.");
        }

        var user = await _identityService.GetUserByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            return ApiResponse<string>.Fail("User not found.");
        }

        // Only users in PendingApproval or DocumentsRequested status can be rejected
        if (user.AccountStatus is not (AccountStatus.PendingApproval or AccountStatus.DocumentsRequested))
        {
            return ApiResponse<string>.Fail(
                $"User cannot be rejected from status '{user.AccountStatus}'.");
        }

        var previousStatus = user.AccountStatus;

        // Set account status to Closed (rejected)
        user.AccountStatus = AccountStatus.Closed;
        user.AccountStatusReason = request.Reason;
        user.AccountStatusChangedAtUtc = DateTime.UtcNow;
        user.AccountStatusChangedBy = _currentUserService.UserId ?? "System";

        // Persist user changes
        var saved = await _identityService.SaveUserAsync(user, cancellationToken);
        if (!saved)
        {
            return ApiResponse<string>.Fail("Failed to update user account.");
        }

        // Send notification to the user
        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            await _emailService.SendAccountStatusChangedAsync(
                user.Email,
                user.FullName,
                previousStatus,
                AccountStatus.Closed,
                request.Reason,
                cancellationToken);
        }

        // Record audit log
        var performedBy = _currentUserService.UserId ?? "System";
        await _auditLogRepository.AddAsync(
            AuditLog.Create(
                "ApplicationUser",
                null,
                "RegistrationRejected",
                $"Registration rejected for user '{user.Email}'. Reason: {request.Reason}.",
                performedBy),
            cancellationToken);

        await _auditLogRepository.SaveChangesAsync(cancellationToken);

        return ApiResponse<string>.Ok(
            request.UserId,
            "Registration rejected.");
    }
}
