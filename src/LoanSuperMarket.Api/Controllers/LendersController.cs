using LoanSuperMarket.Application.Features.Lenders.CreateLender;
using LoanSuperMarket.Application.Features.Lenders.GetLenders;
using LoanSuperMarket.Shared.Common;
using LoanSuperMarket.Shared.Lenders;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LoanSuperMarket.Api.Controllers;

[ApiController]
[Route("api/lenders")]
public sealed class LendersController : ControllerBase
{
    private readonly ISender _sender;

    public LendersController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<LenderDto>>>> GetLenders(
        CancellationToken cancellationToken)
    {
        var lenders = await _sender.Send(new GetLendersQuery(), cancellationToken);

        return Ok(ApiResponse<IReadOnlyList<LenderDto>>.Ok(
            lenders,
            "Lenders retrieved successfully."));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateLender(
        [FromBody] CreateLenderRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateLenderCommand(
            request.CompanyName,
            request.ContactName,
            request.Email,
            request.PhoneNumber,
            request.AvailableFunds);

        var lenderId = await _sender.Send(command, cancellationToken);

        return Ok(ApiResponse<Guid>.Ok(
            lenderId,
            "Lender created successfully."));
    }
}