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

    public LoginCommandHandler(
        IIdentityService identityService,
        ITokenService tokenService,
        ITwoFactorService twoFactorService,
        ISessionService sessionService,
        IAuditLogRepository auditLogRepository)
    {
        _identityService = identityService;
        _tokenService = tokenService;
        _twoFactorService = twoFactorService;
        _sessionService = sessionService;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<ApiResponse<AuthTokenResponse>> Handle(
        LoginCommand request,
        CancellationToken cancellationToken)
    {
        // Validate credentials - returns same generic error for wrong email or wrong password
        var credentialsValid = await _identityService.ValidateCredentialsAsync(
            request.Email,
            request.Password,
            cancellationToken);

        if (!credentialsValid)
        {
            await RecordFailedLoginAuditAsync(request.Email, cancellationToken);
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
            await RecordFailedLoginAuditAsync(request.Email, cancellationToken);
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
                await RecordFailedLoginAuditAsync(request.Email, cancellationToken);
                return ApiResponse<AuthTokenResponse>.Fail(TwoFactorInvalidError);
            }
        }

        // Generate tokens
        var tokenResponse = await _tokenService.GenerateTokensAsync(
            user,
            request.RememberMe,
            cancellationToken);

        // Create session
        var sessionInfo = new SessionInfo(null, null, null);
        await _sessionService.CreateSessionAsync(
            user.Id,
            tokenResponse.RefreshToken,
            sessionInfo,
            cancellationToken);

        // Update last login timestamp
        user.LastLoginAtUtc = DateTime.UtcNow;

        // Record successful login audit
        await RecordSuccessfulLoginAuditAsync(user.Id, request.Email, cancellationToken);

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

    private async Task RecordFailedLoginAuditAsync(string email, CancellationToken cancellationToken)
    {
        var auditLog = AuditLog.Create(
            "ApplicationUser",
            entityId: null,
            "LoginFailed",
            $"Failed login attempt for email: {email}");

        await _auditLogRepository.AddAsync(auditLog, cancellationToken);
    }

    private async Task RecordSuccessfulLoginAuditAsync(
        string userId,
        string email,
        CancellationToken cancellationToken)
    {
        var auditLog = AuditLog.Create(
            "ApplicationUser",
            entityId: null,
            "Login",
            $"Successful login for user: {email}",
            performedBy: userId);

        await _auditLogRepository.AddAsync(auditLog, cancellationToken);
    }
}
