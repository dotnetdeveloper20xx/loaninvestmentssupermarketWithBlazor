using System.ComponentModel.DataAnnotations;

namespace LoanSuperMarket.Application.Features.Auth.Models;

/// <summary>
/// Request model for logging out by revoking a refresh token.
/// </summary>
public sealed class LogoutRequest
{
    [Required(ErrorMessage = "Refresh token is required.")]
    public string RefreshToken { get; set; } = string.Empty;
}
