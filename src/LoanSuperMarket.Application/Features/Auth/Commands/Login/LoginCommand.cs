using LoanSuperMarket.Application.Features.Auth.Models;
using LoanSuperMarket.Shared.Common;
using MediatR;

namespace LoanSuperMarket.Application.Features.Auth.Commands.Login;

/// <summary>
/// Command to authenticate a user with email and password, optionally with 2FA TOTP code.
/// </summary>
public sealed record LoginCommand(
    string Email,
    string Password,
    bool RememberMe,
    string? TotpCode) : IRequest<ApiResponse<AuthTokenResponse>>;
