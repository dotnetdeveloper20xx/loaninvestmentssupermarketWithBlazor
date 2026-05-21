using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Domain.Entities;
using LoanSuperMarket.Shared.Common;
using MediatR;

namespace LoanSuperMarket.Application.Features.Roles.Commands.DeleteCustomRole;

public sealed class DeleteCustomRoleCommandHandler
    : IRequestHandler<DeleteCustomRoleCommand, ApiResponse<string>>
{
    private static readonly HashSet<string> SystemRoleNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Admin", "CrmManager", "CustomerService", "Lender", "Borrower", "Auditor"
    };

    private readonly IRoleManagementService _roleManagementService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditLogRepository _auditLogRepository;

    public DeleteCustomRoleCommandHandler(
        IRoleManagementService roleManagementService,
        ICurrentUserService currentUserService,
        IAuditLogRepository auditLogRepository)
    {
        _roleManagementService = roleManagementService;
        _currentUserService = currentUserService;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<ApiResponse<string>> Handle(
        DeleteCustomRoleCommand request,
        CancellationToken cancellationToken)
    {
        // Validate role exists
        var role = await _roleManagementService.GetRoleByIdAsync(
            request.RoleId, cancellationToken);

        if (role is null)
        {
            return ApiResponse<string>.Fail("Role not found.");
        }

        // Prevent deletion of predefined system roles
        if (role.IsSystemRole || SystemRoleNames.Contains(role.Name ?? string.Empty))
        {
            return ApiResponse<string>.Fail(
                "Cannot delete predefined system roles (Admin, CrmManager, CustomerService, Lender, Borrower, Auditor).");
        }

        var performedBy = _currentUserService.UserId ?? "System";
        var roleName = role.Name;

        // Delete the role (cascade deletes permissions via EF configuration)
        var (succeeded, errors) = await _roleManagementService.DeleteRoleAsync(
            request.RoleId, cancellationToken);

        if (!succeeded)
        {
            return ApiResponse<string>.Fail(errors.ToList());
        }

        // Record audit log
        await _auditLogRepository.AddAsync(
            AuditLog.Create(
                "CustomRole",
                null,
                "Deleted",
                $"Custom role '{roleName}' deleted.",
                performedBy),
            cancellationToken);

        await _auditLogRepository.SaveChangesAsync(cancellationToken);

        return ApiResponse<string>.Ok(
            request.RoleId,
            $"Custom role '{roleName}' deleted successfully.");
    }
}
