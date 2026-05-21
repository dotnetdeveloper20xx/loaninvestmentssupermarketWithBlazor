using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Application.Features.Roles.Models;
using MediatR;

namespace LoanSuperMarket.Application.Features.Roles.Queries.GetRolePermissions;

/// <summary>
/// Handles the GetRolePermissionsQuery by retrieving permissions for the specified role.
/// </summary>
public sealed class GetRolePermissionsQueryHandler
    : IRequestHandler<GetRolePermissionsQuery, IReadOnlyList<PermissionDto>>
{
    private readonly IRoleQueryService _roleQueryService;

    public GetRolePermissionsQueryHandler(IRoleQueryService roleQueryService)
    {
        _roleQueryService = roleQueryService;
    }

    public async Task<IReadOnlyList<PermissionDto>> Handle(
        GetRolePermissionsQuery request,
        CancellationToken cancellationToken)
    {
        return await _roleQueryService.GetRolePermissionsAsync(
            request.RoleId,
            cancellationToken);
    }
}
