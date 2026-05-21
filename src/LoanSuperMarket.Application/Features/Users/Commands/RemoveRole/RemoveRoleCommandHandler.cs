using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Domain.Entities;
using LoanSuperMarket.Shared.Common;
using MediatR;

namespace LoanSuperMarket.Application.Features.Users.Commands.RemoveRole;

public sealed class RemoveRoleCommandHandler
    : IRequestHandler<RemoveRoleCommand, ApiResponse<string>>
{
    private readonly IIdentityService _identityService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditLogRepository _auditLogRepository;

    public RemoveRoleCommandHandler(
        IIdentityService identityService,
        ICurrentUserService currentUserService,
        IAuditLogRepository auditLogRepository)
    {
        _identityService = identityService;
        _currentUserService = currentUserService;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<ApiResponse<string>> Handle(
        RemoveRoleCommand request,
        CancellationToken cancellationToken)
    {
        // Verify the target user exists
        var user = await _identityService.GetUserByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            return ApiResponse<string>.Fail("User not found.");
        }

        // Prevent removal of the last Admin role from the system
        if (string.Equals(request.RoleName, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            var admins = await _identityService.GetUsersInRoleAsync("Admin", cancellationToken);
            if (admins.Count <= 1)
            {
                return ApiResponse<string>.Fail(
                    "Cannot remove the last Admin role from the system. At least one Admin must exist.");
            }
        }

        // Remove the role
        var succeeded = await _identityService.RemoveRoleAsync(
            request.UserId, request.RoleName, cancellationToken);

        if (!succeeded)
        {
            return ApiResponse<string>.Fail($"Failed to remove role '{request.RoleName}' from user.");
        }

        // Record audit log
        var performedBy = _currentUserService.UserId ?? "System";
        await _auditLogRepository.AddAsync(
            AuditLog.Create(
                "ApplicationUser",
                null,
                "RoleChanged",
                $"Role '{request.RoleName}' removed from user '{user.Email}' by '{performedBy}'.",
                performedBy),
            cancellationToken);

        await _auditLogRepository.SaveChangesAsync(cancellationToken);

        return ApiResponse<string>.Ok(
            request.UserId,
            $"Role '{request.RoleName}' removed successfully.");
    }
}
