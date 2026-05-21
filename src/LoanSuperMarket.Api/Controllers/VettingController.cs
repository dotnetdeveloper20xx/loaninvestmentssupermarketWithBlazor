using LoanSuperMarket.Application.Features.Users.Models;
using LoanSuperMarket.Application.Features.Vetting.Commands.ApproveRegistration;
using LoanSuperMarket.Application.Features.Vetting.Commands.RejectRegistration;
using LoanSuperMarket.Application.Features.Vetting.Commands.RequestDocuments;
using LoanSuperMarket.Application.Features.Vetting.Queries.GetVettingQueue;
using LoanSuperMarket.Domain.Enums;
using LoanSuperMarket.Shared.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LoanSuperMarket.Api.Controllers;

[ApiController]
[Route("api/vetting")]
[Authorize(Policy = "CanVetUsers")]
public sealed class VettingController : ControllerBase
{
    private readonly ISender _sender;

    public VettingController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("queue")]
    public async Task<ActionResult<ApiResponse<PagedResult<VettingItemDto>>>> GetVettingQueue(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(
            new GetVettingQueueQuery(page, pageSize),
            cancellationToken);

        return Ok(ApiResponse<PagedResult<VettingItemDto>>.Ok(
            result,
            "Vetting queue retrieved successfully."));
    }

    [HttpPost("{id}/approve")]
    public async Task<ActionResult<ApiResponse<string>>> Approve(
        string id,
        [FromBody] ApproveRegistrationRequest request,
        CancellationToken cancellationToken)
    {
        var command = new ApproveRegistrationCommand(
            id,
            request.Reason,
            request.CreditTier,
            request.CreditLimit,
            request.CapitalLimit);

        var result = await _sender.Send(command, cancellationToken);

        return Ok(result);
    }

    [HttpPost("{id}/reject")]
    public async Task<ActionResult<ApiResponse<string>>> Reject(
        string id,
        [FromBody] RejectRegistrationRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RejectRegistrationCommand(id, request.Reason);

        var result = await _sender.Send(command, cancellationToken);

        return Ok(result);
    }

    [HttpPost("{id}/request-docs")]
    public async Task<ActionResult<ApiResponse<string>>> RequestDocuments(
        string id,
        [FromBody] RequestDocumentsRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RequestDocumentsCommand(id, request.RequiredDocuments);

        var result = await _sender.Send(command, cancellationToken);

        return Ok(result);
    }
}

// Request DTOs for vetting endpoints

public sealed record ApproveRegistrationRequest(
    string Reason,
    CreditTier? CreditTier = null,
    decimal? CreditLimit = null,
    decimal? CapitalLimit = null);

public sealed record RejectRegistrationRequest(string Reason);

public sealed record RequestDocumentsRequest(IReadOnlyList<string> RequiredDocuments);
