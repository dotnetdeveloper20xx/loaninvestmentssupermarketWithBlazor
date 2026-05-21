namespace LoanSuperMarket.Application.Features.Auth.Models;

/// <summary>
/// Response model containing the shared key and QR code URI for TOTP two-factor authentication setup.
/// </summary>
public sealed class TwoFactorSetupResponse
{
    public string SharedKey { get; init; } = string.Empty;

    public string QrCodeUri { get; init; } = string.Empty;
}
