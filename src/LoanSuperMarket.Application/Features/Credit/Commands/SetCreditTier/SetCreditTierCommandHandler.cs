using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Domain.Entities;
using LoanSuperMarket.Shared.Common;
using MediatR;

namespace LoanSuperMarket.Application.Features.Credit.Commands.SetCreditTier;

public sealed class SetCreditTierCommandHandler
    : IRequestHandler<SetCreditTierCommand, ApiResponse<string>>
{
    private readonly IIdentityService _identityService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditLogRepository _auditLogRepository;

    public SetCreditTierCommandHandler(
        IIdentityService identityService,
        ICurrentUserService currentUserService,
        IAuditLogRepository auditLogRepository)
    {
        _identityService = identityService;
        _currentUserService = currentUserService;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<ApiResponse<string>> Handle(
        SetCreditTierCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Justification))
        {
            return ApiResponse<string>.Fail("Justification is required for credit tier changes.");
        }

        var user = await _identityService.GetUserByIdAsync(request.BorrowerUserId, cancellationToken);
        if (user is null)
        {
            return ApiResponse<string>.Fail("User not found.");
        }

        var previousTier = user.CreditTier;

        // Update the credit tier
        user.CreditTier = request.Tier;

        var saved = await _identityService.SaveUserAsync(user, cancellationToken);
        if (!saved)
        {
            return ApiResponse<string>.Fail("Failed to update credit tier.");
        }

        // Record audit log with previous and new values
        var performedBy = _currentUserService.UserId ?? "System";
        var description = $"Credit tier changed for user '{user.Email}'. " +
                          $"Previous: {previousTier?.ToString() ?? "None"}, " +
                          $"New: {request.Tier}. " +
                          $"Justification: {request.Justification}";

        await _auditLogRepository.AddAsync(
            AuditLog.Create(
                "ApplicationUser",
                null,
                "CreditTierChanged",
                description,
                performedBy),
            cancellationToken);

        await _auditLogRepository.SaveChangesAsync(cancellationToken);

        return ApiResponse<string>.Ok(
            request.BorrowerUserId,
            "Credit tier updated successfully.");
    }
}
