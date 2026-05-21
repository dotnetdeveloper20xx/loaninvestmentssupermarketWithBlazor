using System.ComponentModel.DataAnnotations;

namespace LoanSuperMarket.Application.Features.Auth.Models;

/// <summary>
/// Request model for verifying a TOTP code to enable two-factor authentication.
/// </summary>
public sealed class Verify2FaRequest
{
    [Required(ErrorMessage = "Verification code is required.")]
    public string Code { get; set; } = string.Empty;
}
