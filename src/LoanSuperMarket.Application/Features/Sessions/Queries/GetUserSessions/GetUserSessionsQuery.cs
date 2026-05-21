using LoanSuperMarket.Application.Features.Users.Models;
using MediatR;

namespace LoanSuperMarket.Application.Features.Sessions.Queries.GetUserSessions;

/// <summary>
/// Query to retrieve all sessions for a specified user.
/// </summary>
public sealed record GetUserSessionsQuery(string UserId) : IRequest<IReadOnlyList<UserSessionDto>>;
