using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Domain.Entities;
using LoanSuperMarket.Shared.Common;
using MediatR;

namespace LoanSuperMarket.Application.Features.Credit.Commands.SetCapitalLimit;

public sealed class SetCapitalLimitCommandHandler
    : IRequestHandler<SetCapitalLimitCommand, ApiResponse<string>>
{
    private readonly IIdentityService _identityService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditLogRepository _auditLogRepository;

    public SetCapitalLimitCommandHandler(
        IIdentityService identityService,
        ICurrentUserService currentUserService,
        IAuditLogRepository auditLogRepository)
    {
        _identityService = identityService;
        _currentUserService = currentUserService;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<ApiResponse<string>> Handle(
        SetCapitalLimitCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Justification))
        {
            return ApiResponse<string>.Fail("Justification is required for capital limit changes.");
        }

        var user = await _identityService.GetUserByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            return ApiResponse<string>.Fail("User not found.");
        }

        var previousLimit = user.CapitalLimit;

        // Update the capital limit
        user.CapitalLimit = request.Limit;

        var saved = await _identityService.SaveUserAsync(user, cancellationToken);
        if (!saved)
        {
            return ApiResponse<string>.Fail("Failed to update capital limit.");
        }

        // Record audit log with previous and new values
        var performedBy = _currentUserService.UserId ?? "System";
        var description = $"Capital limit changed for user '{user.Email}'. " +
                          $"Previous: {previousLimit?.ToString("C") ?? "None"}, " +
                          $"New: {request.Limit:C}. " +
                          $"Justification: {request.Justification}";

        await _auditLogRepository.AddAsync(
            AuditLog.Create(
                "ApplicationUser",
                null,
                "CapitalLimitChanged",
                description,
                performedBy),
            cancellationToken);

        await _auditLogRepository.SaveChangesAsync(cancellationToken);

        return ApiResponse<string>.Ok(
            request.UserId,
            "Capital limit updated successfully.");
    }
}
