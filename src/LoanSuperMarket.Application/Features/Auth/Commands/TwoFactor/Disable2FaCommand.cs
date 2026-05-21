using LoanSuperMarket.Shared.Common;
using MediatR;

namespace LoanSuperMarket.Application.Features.Auth.Commands.TwoFactor;

/// <summary>
/// Command to disable two-factor authentication for the current user.
/// </summary>
public sealed record Disable2FaCommand : IRequest<ApiResponse<string>>;
