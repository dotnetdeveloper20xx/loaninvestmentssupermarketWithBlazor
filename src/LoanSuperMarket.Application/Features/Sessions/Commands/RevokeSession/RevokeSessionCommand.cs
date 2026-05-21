using LoanSuperMarket.Shared.Common;
using MediatR;

namespace LoanSuperMarket.Application.Features.Sessions.Commands.RevokeSession;

/// <summary>
/// Command to revoke a specific user session and invalidate its associated refresh token.
/// </summary>
public sealed record RevokeSessionCommand(Guid SessionId) : IRequest<ApiResponse<string>>;
