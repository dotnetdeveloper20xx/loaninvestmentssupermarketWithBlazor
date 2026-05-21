namespace LoanSuperMarket.Shared.Configuration;

/// <summary>
/// Configuration model for JWT token generation and validation settings.
/// Bound from the "JwtSettings" section in appsettings.json.
/// </summary>
public sealed class JwtSettings
{
    public const string SectionName = "JwtSettings";

    /// <summary>
    /// HMAC-SHA256 secret key for signing tokens. Must be at least 256 bits (32 bytes).
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public int AccessTokenExpirationMinutes { get; set; } = 15;

    public int RefreshTokenExpirationDays { get; set; } = 7;

    public int RememberMeRefreshTokenExpirationDays { get; set; } = 30;
}
