using LoanSuperMarket.Application.Features.Auth.Models;

namespace LoanSuperMarket.Application.Common.Interfaces;

/// <summary>
/// Service for managing TOTP-based two-factor authentication.
/// </summary>
public interface ITwoFactorService
{
    /// <summary>
    /// Generates a TOTP secret and QR code URI for 2FA setup.
    /// </summary>
    Task<TwoFactorSetupResponse> GenerateSetupAsync(
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies a TOTP code against the user's secret.
    /// </summary>
    Task<bool> VerifyCodeAsync(
        string userId,
        string code,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a set of one-time recovery codes for the user.
    /// </summary>
    Task<IReadOnlyList<string>> GenerateRecoveryCodesAsync(
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates and consumes a single-use recovery code.
    /// </summary>
    Task<bool> ValidateRecoveryCodeAsync(
        string userId,
        string code,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Enables two-factor authentication for the user.
    /// </summary>
    Task EnableAsync(
        string userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Disables two-factor authentication for the user.
    /// </summary>
    Task DisableAsync(
        string userId,
        CancellationToken cancellationToken = default);
}
