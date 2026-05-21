using LoanSuperMarket.Application.Features.Credit.Commands.SetCapitalLimit;
using LoanSuperMarket.Application.Features.Credit.Commands.SetCreditLimit;
using LoanSuperMarket.Application.Features.Credit.Commands.SetCreditTier;
using LoanSuperMarket.Domain.Enums;
using LoanSuperMarket.Shared.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LoanSuperMarket.Api.Controllers;

[ApiController]
[Route("api/credit")]
[Authorize(Policy = "CanSetLimits")]
public sealed class CreditController : ControllerBase
{
    private readonly ISender _sender;

    public CreditController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("{userId}/tier")]
    public async Task<ActionResult<ApiResponse<string>>> SetCreditTier(
        string userId,
        [FromBody] SetCreditTierRequest request,
        CancellationToken cancellationToken)
    {
        var command = new SetCreditTierCommand(userId, request.Tier, request.Justification);

        var result = await _sender.Send(command, cancellationToken);

        return Ok(result);
    }

    [HttpPost("{userId}/credit-limit")]
    public async Task<ActionResult<ApiResponse<string>>> SetCreditLimit(
        string userId,
        [FromBody] SetCreditLimitRequest request,
        CancellationToken cancellationToken)
    {
        var command = new SetCreditLimitCommand(userId, request.Limit, request.Justification);

        var result = await _sender.Send(command, cancellationToken);

        return Ok(result);
    }

    [HttpPost("{userId}/capital-limit")]
    public async Task<ActionResult<ApiResponse<string>>> SetCapitalLimit(
        string userId,
        [FromBody] SetCapitalLimitRequest request,
        CancellationToken cancellationToken)
    {
        var command = new SetCapitalLimitCommand(userId, request.Limit, request.Justification);

        var result = await _sender.Send(command, cancellationToken);

        return Ok(result);
    }
}

// Request DTOs for credit endpoints

public sealed record SetCreditTierRequest(
    CreditTier Tier,
    string Justification);

public sealed record SetCreditLimitRequest(
    decimal Limit,
    string Justification);

public sealed record SetCapitalLimitRequest(
    decimal Limit,
    string Justification);
