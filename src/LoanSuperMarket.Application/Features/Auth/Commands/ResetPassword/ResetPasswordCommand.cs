using LoanSuperMarket.Shared.Common;
using MediatR;

namespace LoanSuperMarket.Application.Features.Auth.Commands.ResetPassword;

/// <summary>
/// Resets a user's password using a valid reset token. On success, revokes all
/// active refresh tokens and records an audit log entry.
/// </summary>
public sealed record ResetPasswordCommand(
    string Email,
    string Token,
    string NewPassword) : IRequest<ApiResponse<string>>;
