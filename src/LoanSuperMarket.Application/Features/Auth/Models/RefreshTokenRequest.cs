using System.ComponentModel.DataAnnotations;

namespace LoanSuperMarket.Application.Features.Auth.Models;

/// <summary>
/// Request model for refreshing an access token using a refresh token.
/// </summary>
public sealed class RefreshTokenRequest
{
    [Required(ErrorMessage = "Refresh token is required.")]
    public string RefreshToken { get; set; } = string.Empty;
}
