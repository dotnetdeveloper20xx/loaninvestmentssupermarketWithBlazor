using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Application.Features.Auth.Models;
using LoanSuperMarket.Domain.Entities.Identity;
using Microsoft.AspNetCore.Identity;

namespace LoanSuperMarket.Infrastructure.Identity;

/// <summary>
/// Service for managing TOTP-based two-factor authentication using ASP.NET Core Identity.
/// </summary>
public sealed class TwoFactorService : ITwoFactorService
{
    private const string Issuer = "LoanSuperMarket";
    private const int RecoveryCodeCount = 10;

    private readonly UserManager<ApplicationUser> _userManager;

    public TwoFactorService(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    /// <inheritdoc />
    public async Task<TwoFactorSetupResponse> GenerateSetupAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var user = await GetUserByIdAsync(userId);

        // Reset the authenticator key to generate a fresh secret
        await _userManager.ResetAuthenticatorKeyAsync(user);

        var sharedKey = await _userManager.GetAuthenticatorKeyAsync(user)
            ?? throw new InvalidOperationException("Failed to generate authenticator key.");

        var email = await _userManager.GetEmailAsync(user)
            ?? throw new InvalidOperationException("User email not found.");

        var qrCodeUri = GenerateQrCodeUri(email, sharedKey);

        return new TwoFactorSetupResponse
        {
            SharedKey = sharedKey,
            QrCodeUri = qrCodeUri
        };
    }

    /// <inheritdoc />
    public async Task<bool> VerifyCodeAsync(
        string userId,
        string code,
        CancellationToken cancellationToken = default)
    {
        var user = await GetUserByIdAsync(userId);

        return await _userManager.VerifyTwoFactorTokenAsync(
            user,
            TokenOptions.DefaultAuthenticatorProvider,
            code);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GenerateRecoveryCodesAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var user = await GetUserByIdAsync(userId);

        var codes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, RecoveryCodeCount)
            ?? throw new InvalidOperationException("Failed to generate recovery codes.");

        return codes.ToList().AsReadOnly();
    }

    /// <inheritdoc />
    public async Task<bool> ValidateRecoveryCodeAsync(
        string userId,
        string code,
        CancellationToken cancellationToken = default)
    {
        var user = await GetUserByIdAsync(userId);

        var result = await _userManager.RedeemTwoFactorRecoveryCodeAsync(user, code);

        return result.Succeeded;
    }

    /// <inheritdoc />
    public async Task EnableAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var user = await GetUserByIdAsync(userId);

        var result = await _userManager.SetTwoFactorEnabledAsync(user, true);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to enable 2FA: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        user.TwoFactorSetupComplete = true;
        await _userManager.UpdateAsync(user);
    }

    /// <inheritdoc />
    public async Task DisableAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var user = await GetUserByIdAsync(userId);

        var result = await _userManager.SetTwoFactorEnabledAsync(user, false);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to disable 2FA: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        user.TwoFactorSetupComplete = false;
        await _userManager.UpdateAsync(user);
    }

    private async Task<ApplicationUser> GetUserByIdAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        return user ?? throw new InvalidOperationException($"User with ID '{userId}' not found.");
    }

    private static string GenerateQrCodeUri(string email, string sharedKey)
    {
        var encodedIssuer = Uri.EscapeDataString(Issuer);
        var encodedEmail = Uri.EscapeDataString(email);

        return $"otpauth://totp/{encodedIssuer}:{encodedEmail}?secret={sharedKey}&issuer={encodedIssuer}";
    }
}
