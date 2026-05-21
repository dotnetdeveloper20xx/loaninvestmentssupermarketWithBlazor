using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Application.Features.Auth.Models;
using LoanSuperMarket.Domain.Entities;
using LoanSuperMarket.Shared.Common;
using MediatR;

namespace LoanSuperMarket.Application.Features.Auth.Commands.Register;

public sealed class RegisterCommandHandler
    : IRequestHandler<RegisterCommand, ApiResponse<string>>
{
    private readonly IIdentityService _identityService;
    private readonly IEmailService _emailService;
    private readonly IAuditLogRepository _auditLogRepository;

    public RegisterCommandHandler(
        IIdentityService identityService,
        IEmailService emailService,
        IAuditLogRepository auditLogRepository)
    {
        _identityService = identityService;
        _emailService = emailService;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<ApiResponse<string>> Handle(
        RegisterCommand request,
        CancellationToken cancellationToken)
    {
        // Create the user via the identity service (sets AccountStatus to PendingApproval internally)
        var registerRequest = new RegisterUserRequest(
            request.Email,
            request.Password,
            request.FirstName,
            request.LastName,
            request.UserType,
            request.CompanyName);

        var (succeeded, userId, errors) = await _identityService.RegisterUserAsync(
            registerRequest, cancellationToken);

        if (!succeeded)
        {
            return ApiResponse<string>.Fail(errors.ToList());
        }

        // Assign the appropriate role based on UserType
        var roleName = request.UserType; // "Borrower" or "Lender"
        await _identityService.AssignRoleAsync(userId, roleName, cancellationToken);

        // Generate email confirmation token and send verification email
        var confirmationToken = await _identityService.GenerateEmailConfirmationTokenAsync(
            userId, cancellationToken);

        await _emailService.SendEmailConfirmationAsync(
            request.Email, userId, confirmationToken, cancellationToken);

        // Record audit log entry for registration
        await _auditLogRepository.AddAsync(
            AuditLog.Create(
                "ApplicationUser",
                null,
                "Registered",
                $"New {request.UserType} registered with email {request.Email}."),
            cancellationToken);

        await _auditLogRepository.SaveChangesAsync(cancellationToken);

        return ApiResponse<string>.Ok(userId, "Registration successful. Please verify your email.");
    }
}
