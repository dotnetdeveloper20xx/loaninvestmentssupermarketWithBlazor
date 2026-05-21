using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Domain.Entities;
using LoanSuperMarket.Shared.Common;
using MediatR;

namespace LoanSuperMarket.Application.Features.Roles.Commands.CreateCustomRole;

public sealed class CreateCustomRoleCommandHandler
    : IRequestHandler<CreateCustomRoleCommand, ApiResponse<string>>
{
    private readonly IRoleManagementService _roleManagementService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditLogRepository _auditLogRepository;

    public CreateCustomRoleCommandHandler(
        IRoleManagementService roleManagementService,
        ICurrentUserService currentUserService,
        IAuditLogRepository auditLogRepository)
    {
        _roleManagementService = roleManagementService;
        _currentUserService = currentUserService;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<ApiResponse<string>> Handle(
        CreateCustomRoleCommand request,
        CancellationToken cancellationToken)
    {
        var performedBy = _currentUserService.UserId ?? "System";

        // Create the custom role (IsSystemRole = false is set by the service)
        var (succeeded, roleId, errors) = await _roleManagementService.CreateRoleAsync(
            request.Name,
            request.Description,
            performedBy,
            cancellationToken);

        if (!succeeded)
        {
            return ApiResponse<string>.Fail(errors.ToList());
        }

        // Assign permissions to the new role
        if (request.Permissions.Count > 0)
        {
            await _roleManagementService.ReplacePermissionsAsync(
                roleId,
                request.Permissions,
                performedBy,
                cancellationToken);
        }

        // Record audit log
        var permissionSummary = string.Join(", ",
            request.Permissions.Select(p => $"{p.Module}.{p.Action}"));

        await _auditLogRepository.AddAsync(
            AuditLog.Create(
                "CustomRole",
                null,
                "Created",
                $"Custom role '{request.Name}' created with permissions: [{permissionSummary}].",
                performedBy),
            cancellationToken);

        await _auditLogRepository.SaveChangesAsync(cancellationToken);

        return ApiResponse<string>.Ok(roleId, $"Custom role '{request.Name}' created successfully.");
    }
}
