using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Domain.Entities;
using LoanSuperMarket.Shared.Common;
using MediatR;

namespace LoanSuperMarket.Application.Features.Roles.Commands.UpdateCustomRole;

public sealed class UpdateCustomRoleCommandHandler
    : IRequestHandler<UpdateCustomRoleCommand, ApiResponse<string>>
{
    private static readonly HashSet<string> SystemRoleNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Admin", "CrmManager", "CustomerService", "Lender", "Borrower", "Auditor"
    };

    private readonly IRoleManagementService _roleManagementService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditLogRepository _auditLogRepository;

    public UpdateCustomRoleCommandHandler(
        IRoleManagementService roleManagementService,
        ICurrentUserService currentUserService,
        IAuditLogRepository auditLogRepository)
    {
        _roleManagementService = roleManagementService;
        _currentUserService = currentUserService;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<ApiResponse<string>> Handle(
        UpdateCustomRoleCommand request,
        CancellationToken cancellationToken)
    {
        // Validate role exists
        var role = await _roleManagementService.GetRoleByIdAsync(
            request.RoleId, cancellationToken);

        if (role is null)
        {
            return ApiResponse<string>.Fail("Role not found.");
        }

        // Prevent modification of system roles
        if (role.IsSystemRole || SystemRoleNames.Contains(role.Name ?? string.Empty))
        {
            return ApiResponse<string>.Fail(
                "Cannot modify predefined system roles.");
        }

        var performedBy = _currentUserService.UserId ?? "System";

        // Update description
        var updated = await _roleManagementService.UpdateRoleDescriptionAsync(
            request.RoleId, request.Description, cancellationToken);

        if (!updated)
        {
            return ApiResponse<string>.Fail("Failed to update role description.");
        }

        // Replace permissions
        await _roleManagementService.ReplacePermissionsAsync(
            request.RoleId,
            request.Permissions,
            performedBy,
            cancellationToken);

        // Record audit log
        var permissionSummary = string.Join(", ",
            request.Permissions.Select(p => $"{p.Module}.{p.Action}"));

        await _auditLogRepository.AddAsync(
            AuditLog.Create(
                "CustomRole",
                null,
                "Updated",
                $"Custom role '{role.Name}' updated. New permissions: [{permissionSummary}].",
                performedBy),
            cancellationToken);

        await _auditLogRepository.SaveChangesAsync(cancellationToken);

        return ApiResponse<string>.Ok(
            request.RoleId,
            $"Custom role '{role.Name}' updated successfully.");
    }
}
