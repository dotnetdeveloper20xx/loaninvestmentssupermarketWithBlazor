using LoanSuperMarket.Shared.Common;
using MediatR;

namespace LoanSuperMarket.Application.Features.Auth.Commands.TwoFactor;

/// <summary>
/// Command to verify a TOTP code and enable two-factor authentication for the current user.
/// On success, 2FA is enabled and a set of one-time recovery codes is returned.
/// </summary>
public sealed record Verify2FaCommand(string Code) : IRequest<ApiResponse<IReadOnlyList<string>>>;
