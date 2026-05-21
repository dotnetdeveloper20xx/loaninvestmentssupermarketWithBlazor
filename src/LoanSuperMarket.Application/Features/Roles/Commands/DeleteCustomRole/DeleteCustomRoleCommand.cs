using LoanSuperMarket.Shared.Common;
using MediatR;

namespace LoanSuperMarket.Application.Features.Roles.Commands.DeleteCustomRole;

/// <summary>
/// Command to delete a custom role. Predefined system roles cannot be deleted.
/// </summary>
public sealed record DeleteCustomRoleCommand(
    string RoleId) : IRequest<ApiResponse<string>>;
