using LoanSuperMarket.Application.Features.Users.Models;
using MediatR;

namespace LoanSuperMarket.Application.Features.Auth.Queries.GetCurrentUser;

/// <summary>
/// Query to retrieve the currently authenticated user's profile and permissions.
/// </summary>
public sealed record GetCurrentUserQuery() : IRequest<CurrentUserDto>;
