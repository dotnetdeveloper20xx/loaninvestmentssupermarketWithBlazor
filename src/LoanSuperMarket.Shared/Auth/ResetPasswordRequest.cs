using System.ComponentModel.DataAnnotations;

namespace LoanSuperMarket.Shared.Auth;

/// <summary>
/// Request model for resetting a user's password using a reset token.
/// </summary>
public sealed class ResetPasswordRequest
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Email address is not valid.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Reset token is required.")]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "New password is required.")]
    [StringLength(128, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 128 characters.")]
    public string NewPassword { get; set; } = string.Empty;
}
