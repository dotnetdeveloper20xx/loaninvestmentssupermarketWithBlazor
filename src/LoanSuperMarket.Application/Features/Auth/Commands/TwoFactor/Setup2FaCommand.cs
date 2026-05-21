using LoanSuperMarket.Application.Features.Auth.Models;
using LoanSuperMarket.Shared.Common;
using MediatR;

namespace LoanSuperMarket.Application.Features.Auth.Commands.TwoFactor;

/// <summary>
/// Command to initiate two-factor authentication setup for the current user.
/// Generates a TOTP secret and returns the shared key and QR code URI.
/// </summary>
public sealed record Setup2FaCommand : IRequest<ApiResponse<TwoFactorSetupResponse>>;
