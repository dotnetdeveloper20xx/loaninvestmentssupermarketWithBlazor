using LoanSuperMarket.Application.Features.Users.Models;
using MediatR;

namespace LoanSuperMarket.Application.Features.Users.Queries.GetUserById;

/// <summary>
/// Query to retrieve detailed user information by user ID.
/// </summary>
public sealed record GetUserByIdQuery(string UserId) : IRequest<UserDetailDto?>;
