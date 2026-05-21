using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Shared.Common;
using MediatR;

namespace LoanSuperMarket.Application.Features.Auth.Commands.ConfirmEmail;

/// <summary>
/// Handles the ConfirmEmail command. Validates the confirmation token
/// and marks the user's email as confirmed via IIdentityService.
/// </summary>
public sealed class ConfirmEmailCommandHandler
    : IRequestHandler<ConfirmEmailCommand, ApiResponse<string>>
{
    private readonly IIdentityService _identityService;

    public ConfirmEmailCommandHandler(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    public async Task<ApiResponse<string>> Handle(
        ConfirmEmailCommand request,
        CancellationToken cancellationToken)
    {
        var succeeded = await _identityService.ConfirmEmailAsync(
            request.UserId,
            request.Token,
            cancellationToken);

        if (!succeeded)
        {
            return ApiResponse<string>.Fail("Invalid or expired email confirmation token.");
        }

        return ApiResponse<string>.Ok(
            "Email address has been confirmed successfully.",
            "Email confirmed.");
    }
}
