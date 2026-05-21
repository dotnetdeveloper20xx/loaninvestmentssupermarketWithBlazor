using System.ComponentModel.DataAnnotations;

namespace LoanSuperMarket.Application.Features.Auth.Models;

/// <summary>
/// Request model for confirming a user's email address.
/// </summary>
public sealed class ConfirmEmailRequest
{
    [Required(ErrorMessage = "User ID is required.")]
    public string UserId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirmation token is required.")]
    public string Token { get; set; } = string.Empty;
}
