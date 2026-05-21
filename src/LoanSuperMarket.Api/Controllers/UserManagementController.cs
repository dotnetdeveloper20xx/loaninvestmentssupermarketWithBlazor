using LoanSuperMarket.Application.Features.Sessions.Commands.RevokeSession;
using LoanSuperMarket.Application.Features.Sessions.Queries.GetUserSessions;
using LoanSuperMarket.Application.Features.Users.Commands.AssignRole;
using LoanSuperMarket.Application.Features.Users.Commands.ChangeAccountStatus;
using LoanSuperMarket.Application.Features.Users.Commands.CreateUser;
using LoanSuperMarket.Application.Features.Users.Commands.RemoveRole;
using LoanSuperMarket.Application.Features.Users.Commands.UpdateUser;
using LoanSuperMarket.Application.Features.Users.Models;
using LoanSuperMarket.Application.Features.Users.Queries.GetUserById;
using LoanSuperMarket.Application.Features.Users.Queries.GetUsers;
using LoanSuperMarket.Domain.Enums;
using LoanSuperMarket.Shared.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LoanSuperMarket.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Policy = "CanManageUsers")]
public sealed class UserManagementController : ControllerBase
{
    private readonly ISender _sender;

    public UserManagementController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<UserDto>>>> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? roleFilter = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(
            new GetUsersQuery(page, pageSize, searchTerm, roleFilter),
            cancellationToken);

        return Ok(ApiResponse<PagedResult<UserDto>>.Ok(
            result,
            "Users retrieved successfully."));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<UserDetailDto>>> GetUserById(
        string id,
        CancellationToken cancellationToken)
    {
        var user = await _sender.Send(new GetUserByIdQuery(id), cancellationToken);

        if (user is null)
            return NotFound(ApiResponse<UserDetailDto>.Fail("User not found."));

        return Ok(ApiResponse<UserDetailDto>.Ok(
            user,
            "User retrieved successfully."));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<string>>> CreateUser(
        [FromBody] CreateUserCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);

        return Ok(result);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<string>>> UpdateUser(
        string id,
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateUserCommand(id, request.FirstName, request.LastName, request.Roles);

        var result = await _sender.Send(command, cancellationToken);

        return Ok(result);
    }

    [HttpPost("{id}/status")]
    public async Task<ActionResult<ApiResponse<string>>> ChangeAccountStatus(
        string id,
        [FromBody] ChangeAccountStatusRequest request,
        CancellationToken cancellationToken)
    {
        var command = new ChangeAccountStatusCommand(id, request.NewStatus, request.Reason, request.BlockedActivity);

        var result = await _sender.Send(command, cancellationToken);

        return Ok(result);
    }

    [HttpPost("{id}/assign-role")]
    public async Task<ActionResult<ApiResponse<string>>> AssignRole(
        string id,
        [FromBody] AssignRoleRequest request,
        CancellationToken cancellationToken)
    {
        var command = new AssignRoleCommand(id, request.RoleName);

        var result = await _sender.Send(command, cancellationToken);

        return Ok(result);
    }

    [HttpPost("{id}/remove-role")]
    public async Task<ActionResult<ApiResponse<string>>> RemoveRole(
        string id,
        [FromBody] RemoveRoleRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RemoveRoleCommand(id, request.RoleName);

        var result = await _sender.Send(command, cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id}/sessions")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<UserSessionDto>>>> GetUserSessions(
        string id,
        CancellationToken cancellationToken)
    {
        var sessions = await _sender.Send(new GetUserSessionsQuery(id), cancellationToken);

        return Ok(ApiResponse<IReadOnlyList<UserSessionDto>>.Ok(
            sessions,
            "User sessions retrieved successfully."));
    }

    [HttpPost("{id}/sessions/{sessionId:guid}/revoke")]
    public async Task<ActionResult<ApiResponse<string>>> RevokeSession(
        string id,
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new RevokeSessionCommand(sessionId), cancellationToken);

        return Ok(result);
    }
}

// Request DTOs for endpoints that need to combine route params with body data

public sealed record UpdateUserRequest(
    string FirstName,
    string LastName,
    IReadOnlyList<string> Roles);

public sealed record ChangeAccountStatusRequest(
    AccountStatus NewStatus,
    string Reason,
    string? BlockedActivity = null);

public sealed record AssignRoleRequest(string RoleName);

public sealed record RemoveRoleRequest(string RoleName);
