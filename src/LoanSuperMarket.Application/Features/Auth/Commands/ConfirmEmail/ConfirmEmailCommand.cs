using LoanSuperMarket.Shared.Common;
using MediatR;

namespace LoanSuperMarket.Application.Features.Auth.Commands.ConfirmEmail;

/// <summary>
/// Confirms a user's email address using the provided confirmation token.
/// </summary>
public sealed record ConfirmEmailCommand(
    string UserId,
    string Token) : IRequest<ApiResponse<string>>;
