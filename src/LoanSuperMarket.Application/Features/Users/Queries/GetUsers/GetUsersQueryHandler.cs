using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Application.Features.Users.Models;
using LoanSuperMarket.Shared.Common;
using MediatR;

namespace LoanSuperMarket.Application.Features.Users.Queries.GetUsers;

/// <summary>
/// Handles the GetUsersQuery by delegating to the user query service for paged results.
/// </summary>
public sealed class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, PagedResult<UserDto>>
{
    private readonly IUserQueryService _userQueryService;

    public GetUsersQueryHandler(IUserQueryService userQueryService)
    {
        _userQueryService = userQueryService;
    }

    public async Task<PagedResult<UserDto>> Handle(
        GetUsersQuery request,
        CancellationToken cancellationToken)
    {
        return await _userQueryService.GetUsersPagedAsync(
            request.Page,
            request.PageSize,
            request.SearchTerm,
            request.RoleFilter,
            cancellationToken);
    }
}
