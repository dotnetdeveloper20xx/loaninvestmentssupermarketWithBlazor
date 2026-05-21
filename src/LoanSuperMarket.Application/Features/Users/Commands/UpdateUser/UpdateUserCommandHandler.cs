using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Domain.Entities;
using LoanSuperMarket.Shared.Common;
using MediatR;

namespace LoanSuperMarket.Application.Features.Users.Commands.UpdateUser;

public sealed class UpdateUserCommandHandler
    : IRequestHandler<UpdateUserCommand, ApiResponse<string>>
{
    private readonly IIdentityService _identityService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditLogRepository _auditLogRepository;

    public UpdateUserCommandHandler(
        IIdentityService identityService,
        ICurrentUserService currentUserService,
        IAuditLogRepository auditLogRepository)
    {
        _identityService = identityService;
        _currentUserService = currentUserService;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<ApiResponse<string>> Handle(
        UpdateUserCommand request,
        CancellationToken cancellationToken)
    {
        // Verify user exists
        var user = await _identityService.GetUserByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            return ApiResponse<string>.Fail("User not found.");
        }

        // Update user details
        var updated = await _identityService.UpdateUserAsync(
            request.UserId, request.FirstName, request.LastName, cancellationToken);

        if (!updated)
        {
            return ApiResponse<string>.Fail("Failed to update user details.");
        }

        // Sync roles: remove roles not in the new list, add roles that are new
        var currentRoles = await _identityService.GetUserRolesAsync(
            request.UserId, cancellationToken);

        var rolesToRemove = currentRoles.Except(request.Roles).ToList();
        var rolesToAdd = request.Roles.Except(currentRoles).ToList();

        foreach (var role in rolesToRemove)
        {
            await _identityService.RemoveRoleAsync(request.UserId, role, cancellationToken);
        }

        foreach (var role in rolesToAdd)
        {
            await _identityService.AssignRoleAsync(request.UserId, role, cancellationToken);
        }

        // Record audit log
        var performedBy = _currentUserService.UserId ?? "System";
        var roleChanges = new List<string>();
        if (rolesToAdd.Count > 0)
            roleChanges.Add($"added: {string.Join(", ", rolesToAdd)}");
        if (rolesToRemove.Count > 0)
            roleChanges.Add($"removed: {string.Join(", ", rolesToRemove)}");

        var roleChangeDescription = roleChanges.Count > 0
            ? $" Role changes: {string.Join("; ", roleChanges)}."
            : string.Empty;

        await _auditLogRepository.AddAsync(
            AuditLog.Create(
                "ApplicationUser",
                null,
                "UserUpdated",
                $"User '{user.Email}' updated. Name: {request.FirstName} {request.LastName}.{roleChangeDescription}",
                performedBy),
            cancellationToken);

        await _auditLogRepository.SaveChangesAsync(cancellationToken);

        return ApiResponse<string>.Ok(request.UserId, "User updated successfully.");
    }
}
