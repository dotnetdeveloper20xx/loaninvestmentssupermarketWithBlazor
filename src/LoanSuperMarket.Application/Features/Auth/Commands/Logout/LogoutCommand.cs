using LoanSuperMarket.Shared.Common;
using MediatR;

namespace LoanSuperMarket.Application.Features.Auth.Commands.Logout;

/// <summary>
/// Command to log out the current user by revoking their refresh token
/// and terminating the associated session.
/// </summary>
public sealed record LogoutCommand(string RefreshToken) : IRequest<ApiResponse<string>>;
