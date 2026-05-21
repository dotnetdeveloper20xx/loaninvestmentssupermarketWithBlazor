using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Domain.Entities;
using LoanSuperMarket.Domain.Enums;
using LoanSuperMarket.Shared.Common;
using MediatR;

namespace LoanSuperMarket.Application.Features.Vetting.Commands.ApproveRegistration;

public sealed class ApproveRegistrationCommandHandler
    : IRequestHandler<ApproveRegistrationCommand, ApiResponse<string>>
{
    private readonly IIdentityService _identityService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IEmailService _emailService;
    private readonly IAuditLogRepository _auditLogRepository;

    public ApproveRegistrationCommandHandler(
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
        ApproveRegistrationCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            return ApiResponse<string>.Fail("A reason is required for approval.");
        }

        var user = await _identityService.GetUserByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            return ApiResponse<string>.Fail("User not found.");
        }

        // Only users in PendingApproval or DocumentsRequested status can be approved
        if (user.AccountStatus is not (AccountStatus.PendingApproval or AccountStatus.DocumentsRequested))
        {
            return ApiResponse<string>.Fail(
                $"User cannot be approved from status '{user.AccountStatus}'.");
        }

        // Determine user type from roles
        var roles = await _identityService.GetUserRolesAsync(request.UserId, cancellationToken);
        var isBorrower = roles.Contains("Borrower");
        var isLender = roles.Contains("Lender");

        // Validate required fields based on user type
        if (isBorrower)
        {
            if (request.CreditTier is null)
            {
                return ApiResponse<string>.Fail(
                    "CreditTier is required when approving a Borrower.");
            }

            if (request.CreditLimit is null)
            {
                return ApiResponse<string>.Fail(
                    "CreditLimit is required when approving a Borrower.");
            }
        }

        if (isLender)
        {
            if (request.CapitalLimit is null)
            {
                return ApiResponse<string>.Fail(
                    "CapitalLimit is required when approving a Lender.");
            }
        }

        var previousStatus = user.AccountStatus;

        // Set account status to Active
        user.AccountStatus = AccountStatus.Active;
        user.AccountStatusReason = request.Reason;
        user.AccountStatusChangedAtUtc = DateTime.UtcNow;
        user.AccountStatusChangedBy = _currentUserService.UserId ?? "System";

        // Set credit/capital limits based on user type
        if (isBorrower)
        {
            user.CreditTier = request.CreditTier;
            user.CreditLimit = request.CreditLimit;
        }

        if (isLender)
        {
            user.CapitalLimit = request.CapitalLimit;
        }

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
                AccountStatus.Active,
                request.Reason,
                cancellationToken);
        }

        // Record audit log
        var performedBy = _currentUserService.UserId ?? "System";
        var description = $"Registration approved for user '{user.Email}'. Reason: {request.Reason}.";

        if (isBorrower)
        {
            description += $" CreditTier: {request.CreditTier}, CreditLimit: {request.CreditLimit}.";
        }

        if (isLender)
        {
            description += $" CapitalLimit: {request.CapitalLimit}.";
        }

        await _auditLogRepository.AddAsync(
            AuditLog.Create(
                "ApplicationUser",
                null,
                "RegistrationApproved",
                description,
                performedBy),
            cancellationToken);

        await _auditLogRepository.SaveChangesAsync(cancellationToken);

        return ApiResponse<string>.Ok(
            request.UserId,
            "Registration approved successfully.");
    }
}
