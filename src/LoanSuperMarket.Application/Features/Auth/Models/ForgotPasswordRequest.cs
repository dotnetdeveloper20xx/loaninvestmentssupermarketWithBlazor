using System.ComponentModel.DataAnnotations;

namespace LoanSuperMarket.Application.Features.Auth.Models;

/// <summary>
/// Request model for initiating a password reset flow.
/// </summary>
public sealed class ForgotPasswordRequest
{
    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress(ErrorMessage = "Email address is not valid.")]
    public string Email { get; set; } = string.Empty;
}
