using System.ComponentModel.DataAnnotations;

namespace LoanSuperMarket.Application.Features.Auth.Models;

/// <summary>
/// Request model for user authentication (login).
/// </summary>
public sealed class LoginRequest
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Email address is not valid.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }

    /// <summary>
    /// TOTP code for two-factor authentication. Required when 2FA is enabled for the account.
    /// </summary>
    public string? TotpCode { get; set; }
}
