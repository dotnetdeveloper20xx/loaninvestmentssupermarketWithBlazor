using LoanSuperMarket.Application.Features.Funding.DeclineFunding;
using LoanSuperMarket.Application.Features.Funding.FundLoan;
using LoanSuperMarket.Application.Features.Funding.GetFundingApplicationDetails;
using LoanSuperMarket.Application.Features.Funding.GetFundingQueue;
using LoanSuperMarket.Shared.Common;
using LoanSuperMarket.Shared.Funding;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LoanSuperMarket.Api.Controllers;

[ApiController]
[Route("api/funding")]
[Authorize(Policy = "CanManageProducts")]
public sealed class FundingController : ControllerBase
{
    private readonly ISender _sender;

    public FundingController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("queue")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<FundingQueueItemDto>>>> GetFundingQueue(
        [FromQuery] string? productTitle,
        [FromQuery] decimal? minAmount,
        [FromQuery] decimal? maxAmount,
        CancellationToken cancellationToken)
    {
        var query = new GetFundingQueueQuery
        {
            ProductTitleFilter = productTitle,
            MinAmount = minAmount,
            MaxAmount = maxAmount
        };

        var result = await _sender.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{applicationId:guid}/details")]
    public async Task<ActionResult<ApiResponse<FundingApplicationDetailDto>>> GetApplicationDetails(
        Guid applicationId,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new GetFundingApplicationDetailsQuery(applicationId),
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("{applicationId:guid}/accept")]
    public async Task<ActionResult<ApiResponse<FundingResultDto>>> AcceptFunding(
        Guid applicationId,
        [FromBody] AcceptFundingRequest request,
        CancellationToken cancellationToken)
    {
        // LenderId would come from the current user's lender profile in production
        // For now, we'll need it from the request context
        var lenderId = await GetCurrentLenderIdAsync(cancellationToken);

        var command = new FundLoanCommand(applicationId, lenderId);
        var result = await _sender.Send(command, cancellationToken);

        return Ok(result);
    }

    [HttpPost("{applicationId:guid}/decline")]
    public async Task<ActionResult<ApiResponse<string>>> DeclineFunding(
        Guid applicationId,
        [FromBody] DeclineFundingRequest request,
        CancellationToken cancellationToken)
    {
        var lenderId = await GetCurrentLenderIdAsync(cancellationToken);

        var command = new DeclineFundingCommand(applicationId, lenderId, request.Reason);
        var result = await _sender.Send(command, cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Placeholder for resolving the current user's lender ID.
    /// In production, this would use ICurrentUserService to resolve the lender profile.
    /// </summary>
    private Task<Guid> GetCurrentLenderIdAsync(CancellationToken cancellationToken)
    {
        // This would be resolved from the authenticated user's claims/profile
        // For now, return empty — the handler will validate
        return Task.FromResult(Guid.Empty);
    }
}
