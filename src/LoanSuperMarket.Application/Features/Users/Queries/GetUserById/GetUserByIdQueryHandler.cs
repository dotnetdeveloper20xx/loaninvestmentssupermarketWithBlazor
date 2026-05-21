using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Application.Features.Users.Models;
using MediatR;

namespace LoanSuperMarket.Application.Features.Users.Queries.GetUserById;

/// <summary>
/// Handles the GetUserByIdQuery by delegating to the user query service.
/// </summary>
public sealed class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, UserDetailDto?>
{
    private readonly IUserQueryService _userQueryService;

    public GetUserByIdQueryHandler(IUserQueryService userQueryService)
    {
        _userQueryService = userQueryService;
    }

    public async Task<UserDetailDto?> Handle(
        GetUserByIdQuery request,
        CancellationToken cancellationToken)
    {
        return await _userQueryService.GetUserByIdAsync(
            request.UserId,
            cancellationToken);
    }
}
