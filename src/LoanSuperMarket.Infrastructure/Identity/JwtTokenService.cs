using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Application.Features.Auth.Models;
using LoanSuperMarket.Domain.Entities.Identity;
using LoanSuperMarket.Shared.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace LoanSuperMarket.Infrastructure.Identity;

/// <summary>
/// JWT token service implementing access token generation, refresh token rotation,
/// and reuse detection for secure stateless authentication.
/// </summary>
public sealed class JwtTokenService : ITokenService
{
    private const int MinimumKeyLengthBytes = 32; // 256 bits

    private readonly AuthIdentityDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly JwtSettings _jwtSettings;
    private readonly SigningCredentials _signingCredentials;

    public JwtTokenService(
        AuthIdentityDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        IOptions<JwtSettings> jwtSettings)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _jwtSettings = jwtSettings.Value;

        ValidateSecretKey(_jwtSettings.SecretKey);

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        _signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
    }

    /// <inheritdoc />
    public async Task<AuthTokenResponse> GenerateTokensAsync(
        ApplicationUser user,
        bool rememberMe = false,
        CancellationToken cancellationToken = default)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var permissions = await GetUserPermissionsAsync(user.Id, roles, cancellationToken);

        var accessToken = GenerateAccessToken(user, roles, permissions);
        var refreshToken = await CreateRefreshTokenAsync(user.Id, rememberMe, cancellationToken);

        var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);

        return new AuthTokenResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken.Token,
            ExpiresAt = expiresAt
        };
    }

    /// <inheritdoc />
    public async Task<AuthTokenResponse> RefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        var storedToken = await _dbContext.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken, cancellationToken);

        if (storedToken is null)
        {
            throw new UnauthorizedAccessException("Invalid refresh token.");
        }

        // Reuse detection: if a revoked token is presented, revoke ALL tokens for that user
        if (storedToken.IsRevoked)
        {
            await RevokeAllUserTokensAsync(
                storedToken.UserId,
                "Attempted reuse of revoked refresh token (potential token theft)",
                cancellationToken);

            throw new UnauthorizedAccessException(
                "Refresh token has been revoked. All sessions have been terminated for security.");
        }

        if (storedToken.IsExpired)
        {
            throw new UnauthorizedAccessException("Refresh token has expired.");
        }

        // Rotate: revoke the current token and issue a new one
        var user = storedToken.User;
        var roles = await _userManager.GetRolesAsync(user);
        var permissions = await GetUserPermissionsAsync(user.Id, roles, cancellationToken);

        // Revoke the old refresh token
        storedToken.RevokedAtUtc = DateTime.UtcNow;
        storedToken.RevokedReason = "Rotated during refresh";

        // Create a new refresh token
        var newRefreshToken = await CreateRefreshTokenAsync(
            user.Id,
            storedToken.IsRememberMe,
            cancellationToken);

        // Link old token to new one for audit trail
        storedToken.ReplacedByToken = newRefreshToken.Token;

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Generate new access token with current roles (reflects any role changes)
        var accessToken = GenerateAccessToken(user, roles, permissions);
        var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);

        return new AuthTokenResponse
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshToken.Token,
            ExpiresAt = expiresAt
        };
    }

    /// <inheritdoc />
    public async Task RevokeTokenAsync(
        string refreshToken,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var storedToken = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken, cancellationToken);

        if (storedToken is null || storedToken.IsRevoked)
        {
            return;
        }

        storedToken.RevokedAtUtc = DateTime.UtcNow;
        storedToken.RevokedReason = reason;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task RevokeAllUserTokensAsync(
        string userId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var activeTokens = await _dbContext.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;

        foreach (var token in activeTokens)
        {
            token.RevokedAtUtc = now;
            token.RevokedReason = reason;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Validates that the JWT secret key meets the minimum length requirement (256 bits).
    /// Throws at startup if the key is too short.
    /// </summary>
    private static void ValidateSecretKey(string secretKey)
    {
        if (string.IsNullOrWhiteSpace(secretKey))
        {
            throw new InvalidOperationException(
                "JWT secret key is not configured. Please set 'JwtSettings:SecretKey' in application settings.");
        }

        var keyBytes = Encoding.UTF8.GetBytes(secretKey);
        if (keyBytes.Length < MinimumKeyLengthBytes)
        {
            throw new InvalidOperationException(
                $"JWT secret key must be at least {MinimumKeyLengthBytes * 8} bits ({MinimumKeyLengthBytes} bytes). " +
                $"The configured key is only {keyBytes.Length * 8} bits ({keyBytes.Length} bytes).");
        }
    }

    /// <summary>
    /// Generates a signed JWT access token containing user claims, roles, and permissions.
    /// </summary>
    private string GenerateAccessToken(
        ApplicationUser user,
        IList<string> roles,
        IReadOnlyList<string> permissions)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.GivenName, user.FirstName),
            new(JwtRegisteredClaimNames.FamilyName, user.LastName),
            new("account_status", user.AccountStatus.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        // Add role claims
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        // Add permission claims (format: "Module.Action")
        foreach (var permission in permissions)
        {
            claims.Add(new Claim("permissions", permission));
        }

        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expires,
            NotBefore = now,
            IssuedAt = now,
            Issuer = _jwtSettings.Issuer,
            Audience = _jwtSettings.Audience,
            SigningCredentials = _signingCredentials
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }

    /// <summary>
    /// Creates a cryptographically secure refresh token and persists it to the database.
    /// </summary>
    private async Task<RefreshToken> CreateRefreshTokenAsync(
        string userId,
        bool rememberMe,
        CancellationToken cancellationToken)
    {
        var expirationDays = rememberMe
            ? _jwtSettings.RememberMeRefreshTokenExpirationDays
            : _jwtSettings.RefreshTokenExpirationDays;

        var refreshToken = new RefreshToken
        {
            Token = GenerateSecureRandomToken(),
            UserId = userId,
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(expirationDays),
            IsRememberMe = rememberMe
        };

        _dbContext.RefreshTokens.Add(refreshToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return refreshToken;
    }

    /// <summary>
    /// Generates a cryptographically secure random token string for use as a refresh token.
    /// </summary>
    private static string GenerateSecureRandomToken()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(randomBytes);
    }

    /// <summary>
    /// Retrieves the effective permissions for a user based on all their assigned roles.
    /// Permissions are returned in "Module.Action" format.
    /// </summary>
    private async Task<IReadOnlyList<string>> GetUserPermissionsAsync(
        string userId,
        IList<string> roles,
        CancellationToken cancellationToken)
    {
        if (roles.Count == 0)
        {
            return [];
        }

        var roleIds = await _dbContext.Roles
            .Where(r => roles.Contains(r.Name!))
            .Select(r => r.Id)
            .ToListAsync(cancellationToken);

        var permissions = await _dbContext.RolePermissions
            .Where(rp => roleIds.Contains(rp.RoleId))
            .Select(rp => $"{rp.Module}.{rp.Action}")
            .Distinct()
            .ToListAsync(cancellationToken);

        return permissions;
    }
}
