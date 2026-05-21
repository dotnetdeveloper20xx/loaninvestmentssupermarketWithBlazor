using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Domain.Entities;
using LoanSuperMarket.Shared.Common;
using MediatR;

namespace LoanSuperMarket.Application.Features.Users.Commands.AssignRole;

public sealed class AssignRoleCommandHandler
    : IRequestHandler<AssignRoleCommand, ApiResponse<string>>
{
    private readonly IIdentityService _identityService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditLogRepository _auditLogRepository;

    public AssignRoleCommandHandler(
        IIdentityService identityService,
        ICurrentUserService currentUserService,
        IAuditLogRepository auditLogRepository)
    {
        _identityService = identityService;
        _currentUserService = currentUserService;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<ApiResponse<string>> Handle(
        AssignRoleCommand request,
        CancellationToken cancellationToken)
    {
        // Verify the target user exists
        var user = await _identityService.GetUserByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            return ApiResponse<string>.Fail("User not found.");
        }

        // Assign the role
        var succeeded = await _identityService.AssignRoleAsync(
            request.UserId, request.RoleName, cancellationToken);

        if (!succeeded)
        {
            return ApiResponse<string>.Fail($"Failed to assign role '{request.RoleName}' to user.");
        }

        // Record audit log
        var performedBy = _currentUserService.UserId ?? "System";
        await _auditLogRepository.AddAsync(
            AuditLog.Create(
                "ApplicationUser",
                null,
                "RoleChanged",
                $"Role '{request.RoleName}' assigned to user '{user.Email}' by '{performedBy}'.",
                performedBy),
            cancellationToken);

        await _auditLogRepository.SaveChangesAsync(cancellationToken);

        return ApiResponse<string>.Ok(
            request.UserId,
            $"Role '{request.RoleName}' assigned successfully.");
    }
}
