using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Application.Features.Roles.Models;
using MediatR;

namespace LoanSuperMarket.Application.Features.Roles.Queries.GetRoles;

/// <summary>
/// Handles the GetRolesQuery by delegating to the role query service.
/// </summary>
public sealed class GetRolesQueryHandler : IRequestHandler<GetRolesQuery, IReadOnlyList<RoleDto>>
{
    private readonly IRoleQueryService _roleQueryService;

    public GetRolesQueryHandler(IRoleQueryService roleQueryService)
    {
        _roleQueryService = roleQueryService;
    }

    public async Task<IReadOnlyList<RoleDto>> Handle(
        GetRolesQuery request,
        CancellationToken cancellationToken)
    {
        return await _roleQueryService.GetAllRolesAsync(cancellationToken);
    }
}
