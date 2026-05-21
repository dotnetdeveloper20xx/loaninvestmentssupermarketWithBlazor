using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Domain.Entities;
using LoanSuperMarket.Domain.Enums;
using LoanSuperMarket.Shared.Common;
using MediatR;

namespace LoanSuperMarket.Application.Features.Vetting.Commands.RequestDocuments;

public sealed class RequestDocumentsCommandHandler
    : IRequestHandler<RequestDocumentsCommand, ApiResponse<string>>
{
    private readonly IIdentityService _identityService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IEmailService _emailService;
    private readonly IAuditLogRepository _auditLogRepository;

    public RequestDocumentsCommandHandler(
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
        RequestDocumentsCommand request,
        CancellationToken cancellationToken)
    {
        if (request.RequiredDocuments is null || request.RequiredDocuments.Count == 0)
        {
            return ApiResponse<string>.Fail(
                "At least one required document must be specified.");
        }

        var user = await _identityService.GetUserByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            return ApiResponse<string>.Fail("User not found.");
        }

        // Only users in PendingApproval status can have documents requested
        if (user.AccountStatus is not AccountStatus.PendingApproval)
        {
            return ApiResponse<string>.Fail(
                $"Documents can only be requested from users in PendingApproval status. Current status: '{user.AccountStatus}'.");
        }

        var previousStatus = user.AccountStatus;

        // Set account status to DocumentsRequested
        user.AccountStatus = AccountStatus.DocumentsRequested;
        user.AccountStatusReason = $"Documents requested: {string.Join(", ", request.RequiredDocuments)}";
        user.AccountStatusChangedAtUtc = DateTime.UtcNow;
        user.AccountStatusChangedBy = _currentUserService.UserId ?? "System";

        // Persist user changes
        var saved = await _identityService.SaveUserAsync(user, cancellationToken);
        if (!saved)
        {
            return ApiResponse<string>.Fail("Failed to update user account.");
        }

        // Send notification to the applicant with the list of required documents
        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            await _emailService.SendDocumentsRequestedAsync(
                user.Email,
                user.FullName,
                request.RequiredDocuments,
                cancellationToken);
        }

        // Record audit log
        var performedBy = _currentUserService.UserId ?? "System";
        var documentList = string.Join(", ", request.RequiredDocuments);
        await _auditLogRepository.AddAsync(
            AuditLog.Create(
                "ApplicationUser",
                null,
                "DocumentsRequested",
                $"Documents requested from user '{user.Email}': {documentList}.",
                performedBy),
            cancellationToken);

        await _auditLogRepository.SaveChangesAsync(cancellationToken);

        return ApiResponse<string>.Ok(
            request.UserId,
            "Documents requested from applicant.");
    }
}
