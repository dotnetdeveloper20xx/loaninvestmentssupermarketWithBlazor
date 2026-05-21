using LoanSuperMarket.Application.Features.Roles.Commands.CreateCustomRole;
using LoanSuperMarket.Application.Features.Roles.Commands.DeleteCustomRole;
using LoanSuperMarket.Application.Features.Roles.Commands.UpdateCustomRole;
using LoanSuperMarket.Application.Features.Roles.Models;
using LoanSuperMarket.Application.Features.Roles.Queries.GetRolePermissions;
using LoanSuperMarket.Application.Features.Roles.Queries.GetRoles;
using LoanSuperMarket.Application.Features.Roles.Queries.SimulatePermissions;
using LoanSuperMarket.Shared.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LoanSuperMarket.Api.Controllers;

[ApiController]
[Route("api/roles")]
[Authorize(Policy = "CanManageUsers")]
public sealed class RoleController : ControllerBase
{
    private readonly ISender _sender;

    public RoleController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RoleDto>>>> GetRoles(
        CancellationToken cancellationToken)
    {
        var roles = await _sender.Send(new GetRolesQuery(), cancellationToken);

        return Ok(ApiResponse<IReadOnlyList<RoleDto>>.Ok(
            roles,
            "Roles retrieved successfully."));
    }

    [HttpGet("{id}/permissions")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PermissionDto>>>> GetRolePermissions(
        string id,
        CancellationToken cancellationToken)
    {
        var permissions = await _sender.Send(new GetRolePermissionsQuery(id), cancellationToken);

        return Ok(ApiResponse<IReadOnlyList<PermissionDto>>.Ok(
            permissions,
            "Role permissions retrieved successfully."));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<string>>> CreateRole(
        [FromBody] CreateCustomRoleCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);

        return Ok(result);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<string>>> UpdateRole(
        string id,
        [FromBody] UpdateCustomRoleRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateCustomRoleCommand(id, request.Description, request.Permissions);

        var result = await _sender.Send(command, cancellationToken);

        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse<string>>> DeleteRole(
        string id,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new DeleteCustomRoleCommand(id), cancellationToken);

        return Ok(result);
    }

    [HttpPost("simulate/{userId}")]
    public async Task<ActionResult<ApiResponse<PermissionSimulationResult>>> SimulatePermissions(
        string userId,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new SimulatePermissionsQuery(userId), cancellationToken);

        return Ok(ApiResponse<PermissionSimulationResult>.Ok(
            result,
            "Permission simulation completed successfully."));
    }
}

// Request DTO for update endpoint that combines route param with body data

public sealed record UpdateCustomRoleRequest(
    string Description,
    IReadOnlyList<PermissionDto> Permissions);
