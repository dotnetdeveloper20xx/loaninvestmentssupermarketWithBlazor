using LoanSuperMarket.Application.Features.Auth.Models;
using LoanSuperMarket.Shared.Common;
using MediatR;

namespace LoanSuperMarket.Application.Features.Auth.Commands.RefreshToken;

/// <summary>
/// Command to refresh an expired access token using a valid refresh token.
/// Implements token rotation: the old refresh token is revoked and a new pair is issued.
/// Implements reuse detection: if a revoked token is presented, all user tokens are revoked.
/// </summary>
public sealed record RefreshTokenCommand(string RefreshToken) : IRequest<ApiResponse<AuthTokenResponse>>;
