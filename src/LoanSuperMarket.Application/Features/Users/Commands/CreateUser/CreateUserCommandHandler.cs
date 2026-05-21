using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Application.Features.Auth.Models;
using LoanSuperMarket.Domain.Entities;
using LoanSuperMarket.Shared.Common;
using MediatR;

namespace LoanSuperMarket.Application.Features.Users.Commands.CreateUser;

public sealed class CreateUserCommandHandler
    : IRequestHandler<CreateUserCommand, ApiResponse<string>>
{
    private readonly IIdentityService _identityService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IAuditLogRepository _auditLogRepository;

    public CreateUserCommandHandler(
        IIdentityService identityService,
        ICurrentUserService currentUserService,
        IAuditLogRepository auditLogRepository)
    {
        _identityService = identityService;
        _currentUserService = currentUserService;
        _auditLogRepository = auditLogRepository;
    }

    public async Task<ApiResponse<string>> Handle(
        CreateUserCommand request,
        CancellationToken cancellationToken)
    {
        // Register the user via the identity service
        var registerRequest = new RegisterUserRequest(
            request.Email,
            request.Password,
            request.FirstName,
            request.LastName,
            UserType: request.Roles.Count > 0 ? request.Roles[0] : "Borrower",
            CompanyName: null);

        var (succeeded, userId, errors) = await _identityService.RegisterUserAsync(
            registerRequest, cancellationToken);

        if (!succeeded)
        {
            return ApiResponse<string>.Fail(errors.ToList());
        }

        // Assign all specified roles
        foreach (var role in request.Roles)
        {
            var roleAssigned = await _identityService.AssignRoleAsync(
                userId, role, cancellationToken);

            if (!roleAssigned)
            {
                return ApiResponse<string>.Fail($"Failed to assign role '{role}' to user.");
            }
        }

        // Record audit log
        var performedBy = _currentUserService.UserId ?? "System";
        await _auditLogRepository.AddAsync(
            AuditLog.Create(
                "ApplicationUser",
                null,
                "UserCreated",
                $"User '{request.Email}' created with roles: {string.Join(", ", request.Roles)}.",
                performedBy),
            cancellationToken);

        await _auditLogRepository.SaveChangesAsync(cancellationToken);

        return ApiResponse<string>.Ok(userId, "User created successfully.");
    }
}
