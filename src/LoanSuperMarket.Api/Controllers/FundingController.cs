using LoanSuperMarket.Application.Common.Interfaces;
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
    private readonly ICurrentUserService _currentUserService;
    private readonly ILenderRepository _lenderRepository;

    public FundingController(
        ISender sender,
        ICurrentUserService currentUserService,
        ILenderRepository lenderRepository)
    {
        _sender = sender;
        _currentUserService = currentUserService;
        _lenderRepository = lenderRepository;
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
        CancellationToken cancellationToken)
    {
        var lenderId = await GetCurrentLenderIdAsync(cancellationToken);
        if (lenderId is null)
        {
            return Ok(ApiResponse<FundingResultDto>.Fail(
                "No lender profile found for the current user."));
        }

        var command = new FundLoanCommand(applicationId, lenderId.Value);
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
        if (lenderId is null)
        {
            return Ok(ApiResponse<string>.Fail(
                "No lender profile found for the current user."));
        }

        var command = new DeclineFundingCommand(applicationId, lenderId.Value, request.Reason);
        var result = await _sender.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpPost("top-up")]
    public async Task<ActionResult<ApiResponse<decimal>>> TopUpFunds(
        [FromBody] TopUpFundsRequest request,
        CancellationToken cancellationToken)
    {
        var lenderId = await GetCurrentLenderIdAsync(cancellationToken);
        if (lenderId is null)
        {
            return Ok(ApiResponse<decimal>.Fail(
                "No lender profile found for the current user."));
        }

        var command = new Application.Features.Funding.TopUpFunds.TopUpFundsCommand(
            lenderId.Value, request.Amount);
        var result = await _sender.Send(command, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{scheduleId:guid}/restructure")]
    public async Task<ActionResult<ApiResponse<RestructureResultDto>>> RestructureLoan(
        Guid scheduleId,
        [FromBody] RestructureLoanRequest request,
        CancellationToken cancellationToken)
    {
        var command = new Application.Features.Funding.RestructureLoan.RestructureLoanCommand(
            scheduleId, request.NewAnnualRate, request.NewTermMonths, request.Reason);
        var result = await _sender.Send(command, cancellationToken);
        return Ok(result);
    }

    private async Task<Guid?> GetCurrentLenderIdAsync(CancellationToken cancellationToken)
    {
        if (!_currentUserService.IsAuthenticated || string.IsNullOrEmpty(_currentUserService.UserId))
            return null;

        var lender = await _lenderRepository.GetByUserIdAsync(
            _currentUserService.UserId, cancellationToken);

        return lender?.Id;
    }
}
