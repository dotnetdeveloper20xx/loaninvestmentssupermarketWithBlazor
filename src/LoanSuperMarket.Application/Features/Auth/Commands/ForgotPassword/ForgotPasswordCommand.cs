using LoanSuperMarket.Shared.Common;
using MediatR;

namespace LoanSuperMarket.Application.Features.Auth.Commands.ForgotPassword;

/// <summary>
/// Initiates a password reset flow by generating a time-limited reset token
/// and sending a reset email. Always returns success to prevent email enumeration.
/// </summary>
public sealed record ForgotPasswordCommand(string Email) : IRequest<ApiResponse<string>>;
