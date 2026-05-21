using LoanSuperMarket.Application.Features.Users.Models;
using LoanSuperMarket.Shared.Common;
using MediatR;

namespace LoanSuperMarket.Application.Features.Users.Queries.GetUsers;

/// <summary>
/// Query to retrieve a paged list of users with optional search and role filter.
/// </summary>
public sealed record GetUsersQuery(
    int Page,
    int PageSize,
    string? SearchTerm,
    string? RoleFilter) : IRequest<PagedResult<UserDto>>;
