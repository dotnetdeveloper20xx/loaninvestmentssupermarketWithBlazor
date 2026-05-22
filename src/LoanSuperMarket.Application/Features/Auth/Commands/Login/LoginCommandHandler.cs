using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Application.Features.Auth.Models;
using LoanSuperMarket.Domain.Entities;
using LoanSuperMarket.Domain.Enums;
using LoanSuperMarket.Shared.Common;
using MediatR;

namespace LoanSuperMarket.Application.Features.Auth.Commands.Login;

/// <summary>
/// Handles user login: validates credentials, checks account status, handles 2FA,
/// generates tokens, creates session, and records audit log.
/// </summary>
public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, ApiResponse<AuthTokenResponse>>
{
    private const string GenericLoginError = "Invalid email or password.";
    private const string AccountSuspendedError = "Your account has been suspended. Please contact support.";
    private const string AccountClosedError = "Your account has been permanently closed.";
    private const string AccountPendingError = "Your account is pending approval.";
    private const string TwoFactorRequiredError = "Two-factor authentication code is required.";
    private const string TwoFactorInvalidError = "Invalid two-factor authentication code.";
    private const string TwoFactorSetupRequiredError = "Two-factor authentication setup is required.";

    private readonly IIdentityService _identityService;
    private readonly ITokenService _tokenService;
    private readonly ITwoFactorService _twoFactorService;
    private readonly ISessionService _sessionService;
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IClientInfoProvider _clientInfoProvider;

    public LoginCommandHandler(
        IIdentityService identityService,
        ITokenService tokenService,
        ITwoFactorService twoFactorService,
        ISessionService sessionService,
        IAuditLogRepository auditLogRepository,
        IClientInfoProvider clientInfoProvider)
    {
        _identityService = identityService;
        _tokenService = tokenService;
        _twoFactorService = twoFactorService;
        _sessionService = sessionService;
        _auditLogRepository = auditLogRepository;
        _clientInfoProvider = clientInfoProvider;
    }

    public async Task<ApiResponse<AuthTokenResponse>> Handle(
        LoginCommand request,
        CancellationToken cancellationToken)
    {
        var ipAddress = _clientInfoProvider.IpAddress ?? "unknown";

        // Validate credentials - returns same generic error for wrong email or wrong password
        var credentialsValid = await _identityService.ValidateCredentialsAsync(
            request.Email,
            request.Password,
            cancellationToken);

        if (!credentialsValid)
        {
            await RecordFailedLoginAuditAsync(request.Email, ipAddress, cancellationToken);

            // Check if the failed attempt triggered a lockout
            var isLockedOut = await _identityService.IsLockedOutAsync(
                request.Email,
                cancellationToken);

            if (isLockedOut)
            {
                var lockoutEnd = await _identityService.GetLockoutEndDateAsync(
                    request.Email,
                    cancellationToken);

                var duration = lockoutEnd.HasValue
                    ? $"{(lockoutEnd.Value - DateTimeOffset.UtcNow).TotalMinutes:F0} minutes"
                    : "unknown duration";

                await RecordAccountLockedAuditAsync(request.Email, ipAddress, duration, cancellationToken);
            }

            return ApiResponse<AuthTokenResponse>.Fail(GenericLoginError);
        }

        // Get user details
        var user = await _identityService.GetUserByEmailAsync(request.Email, cancellationToken);

        if (user is null)
        {
            // Should not happen if credentials are valid, but guard against it
            return ApiResponse<AuthTokenResponse>.Fail(GenericLoginError);
        }

        // Check account status - reject disabled/suspended/closed/pending
        var statusCheckResult = CheckAccountStatus(user.AccountStatus);
        if (statusCheckResult is not null)
        {
            await RecordFailedLoginAuditAsync(request.Email, ipAddress, cancellationToken);
            return ApiResponse<AuthTokenResponse>.Fail(statusCheckResult);
        }

        // Handle 2FA flow
        if (user.TwoFactorEnabled)
        {
            if (!user.TwoFactorSetupComplete)
            {
                // 2FA is mandatory but not yet configured - redirect to setup
                return ApiResponse<AuthTokenResponse>.Fail(TwoFactorSetupRequiredError);
            }

            if (string.IsNullOrWhiteSpace(request.TotpCode))
            {
                // 2FA enabled but no code provided - require code
                return ApiResponse<AuthTokenResponse>.Fail(TwoFactorRequiredError);
            }

            // Verify the TOTP code
            var codeValid = await _twoFactorService.VerifyCodeAsync(
                user.Id,
                request.TotpCode,
                cancellationToken);

            if (!codeValid)
            {
                await RecordFailedLoginAuditAsync(request.Email, ipAddress, cancellationToken);
                return ApiResponse<AuthTokenResponse>.Fail(TwoFactorInvalidError);
            }
        }

        // Generate tokens
        var tokenResponse = await _tokenService.GenerateTokensAsync(
            user,
            request.RememberMe,
            cancellationToken);

        // Create session
        var sessionInfo = new SessionInfo(null, ipAddress, null);
        await _sessionService.CreateSessionAsync(
            user.Id,
            tokenResponse.RefreshToken,
            sessionInfo,
            cancellationToken);

        // Update last login timestamp
        user.LastLoginAtUtc = DateTime.UtcNow;

        // Record successful login audit
        await RecordSuccessfulLoginAuditAsync(user.Id, request.Email, ipAddress, cancellationToken);

        return ApiResponse<AuthTokenResponse>.Ok(tokenResponse, "Login successful.");
    }

    private static string? CheckAccountStatus(AccountStatus status)
    {
        return status switch
        {
            AccountStatus.Suspended => AccountSuspendedError,
            AccountStatus.Closed => AccountClosedError,
            AccountStatus.PendingApproval => AccountPendingError,
            _ => null
        };
    }

    private async Task RecordFailedLoginAuditAsync(string email, string ipAddress, CancellationToken cancellationToken)
    {
        var auditLog = AuditLog.Create(
            "ApplicationUser",
            entityId: null,
            "LoginFailed",
            $"Failed login attempt for email: {email}, IP: {ipAddress}");

        await _auditLogRepository.AddAsync(auditLog, cancellationToken);
    }

    private async Task RecordSuccessfulLoginAuditAsync(
        string userId,
        string email,
        string ipAddress,
        CancellationToken cancellationToken)
    {
        var auditLog = AuditLog.Create(
            "ApplicationUser",
            entityId: null,
            "Login",
            $"Successful login for user: {email}, IP: {ipAddress}",
            performedBy: userId);

        await _auditLogRepository.AddAsync(auditLog, cancellationToken);
    }

    private async Task RecordAccountLockedAuditAsync(
        string email,
        string ipAddress,
        string duration,
        CancellationToken cancellationToken)
    {
        var auditLog = AuditLog.Create(
            "ApplicationUser",
            entityId: null,
            "AccountLocked",
            $"Account locked for email: {email}, duration: {duration}, IP: {ipAddress}");

        await _auditLogRepository.AddAsync(auditLog, cancellationToken);
    }
}
