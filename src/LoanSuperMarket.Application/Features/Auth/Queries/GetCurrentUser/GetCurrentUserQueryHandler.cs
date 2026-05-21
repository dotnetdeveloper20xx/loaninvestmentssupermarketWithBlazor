using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Application.Features.Users.Models;
using MediatR;

namespace LoanSuperMarket.Application.Features.Auth.Queries.GetCurrentUser;

/// <summary>
/// Handles the GetCurrentUserQuery by combining current user service data with identity service data.
/// </summary>
public sealed class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, CurrentUserDto>
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IIdentityService _identityService;
    private readonly IPermissionResolver _permissionResolver;

    public GetCurrentUserQueryHandler(
        ICurrentUserService currentUserService,
        IIdentityService identityService,
        IPermissionResolver permissionResolver)
    {
        _currentUserService = currentUserService;
        _identityService = identityService;
        _permissionResolver = permissionResolver;
    }

    public async Task<CurrentUserDto> Handle(
        GetCurrentUserQuery request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userId))
        {
            return new CurrentUserDto();
        }

        var user = await _identityService.GetUserByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return new CurrentUserDto();
        }

        var roles = await _identityService.GetUserRolesAsync(userId, cancellationToken);
        var permissions = await _permissionResolver.GetEffectivePermissionsAsync(userId);

        return new CurrentUserDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            FullName = user.FullName,
            Roles = roles,
            Permissions = permissions,
            AccountStatus = user.AccountStatus.ToString(),
            TwoFactorEnabled = user.TwoFactorSetupComplete
        };
    }
}
