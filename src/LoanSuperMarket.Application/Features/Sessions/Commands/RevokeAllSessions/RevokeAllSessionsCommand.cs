using LoanSuperMarket.Shared.Common;
using MediatR;

namespace LoanSuperMarket.Application.Features.Sessions.Commands.RevokeAllSessions;

/// <summary>
/// Command to revoke all sessions for a user and invalidate their associated refresh tokens.
/// If UserId is not provided, defaults to the current authenticated user.
/// </summary>
public sealed record RevokeAllSessionsCommand(string? UserId = null) : IRequest<ApiResponse<string>>;
