using LoanSuperMarket.Domain.Enums;
using LoanSuperMarket.Shared.Common;
using MediatR;

namespace LoanSuperMarket.Application.Features.Users.Commands.ChangeAccountStatus;

/// <summary>
/// Command to change a user's account status with a mandatory reason.
/// </summary>
public sealed record ChangeAccountStatusCommand(
    string UserId,
    AccountStatus NewStatus,
    string Reason,
    string? BlockedActivity = null) : IRequest<ApiResponse<string>>;
