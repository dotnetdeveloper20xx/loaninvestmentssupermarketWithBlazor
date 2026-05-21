using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Shared.Common;
using MediatR;

namespace LoanSuperMarket.Application.Features.Auth.Commands.ForgotPassword;

/// <summary>
/// Handles the ForgotPassword command. Generates a time-limited password reset token
/// and sends a reset email if the user exists. Always returns success to prevent
/// email enumeration attacks (Requirement 5.2).
/// </summary>
public sealed class ForgotPasswordCommandHandler
    : IRequestHandler<ForgotPasswordCommand, ApiResponse<string>>
{
    private readonly IIdentityService _identityService;
    private readonly IEmailService _emailService;

    public ForgotPasswordCommandHandler(
        IIdentityService identityService,
        IEmailService emailService)
    {
        _identityService = identityService;
        _emailService = emailService;
    }

    public async Task<ApiResponse<string>> Handle(
        ForgotPasswordCommand request,
        CancellationToken cancellationToken)
    {
        // Always return success regardless of whether the email exists (prevent enumeration)
        var user = await _identityService.GetUserByEmailAsync(request.Email, cancellationToken);

        if (user is not null)
        {
            // Generate a time-limited reset token (1 hour expiry is configured in Identity options)
            var resetToken = await _identityService.GeneratePasswordResetTokenAsync(
                request.Email,
                cancellationToken);

            // Send the password reset email
            await _emailService.SendPasswordResetAsync(
                request.Email,
                resetToken,
                cancellationToken);
        }

        // Return identical success response for all cases to prevent email enumeration
        return ApiResponse<string>.Ok(
            "Password reset instructions have been sent if the email is registered.",
            "Password reset requested successfully.");
    }
}
