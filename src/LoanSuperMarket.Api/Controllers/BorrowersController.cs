using LoanSuperMarket.Application.Features.Borrowers.CreateBorrower;
using LoanSuperMarket.Application.Features.Borrowers.GetBorrowers;
using LoanSuperMarket.Shared.Borrowers;
using LoanSuperMarket.Shared.Common;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LoanSuperMarket.Api.Controllers;

[ApiController]
[Route("api/borrowers")]
public sealed class BorrowersController : ControllerBase
{
    private readonly ISender _sender;

    public BorrowersController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<BorrowerDto>>>> GetBorrowers(
        CancellationToken cancellationToken)
    {
        var borrowers = await _sender.Send(new GetBorrowersQuery(), cancellationToken);

        return Ok(ApiResponse<IReadOnlyList<BorrowerDto>>.Ok(
            borrowers,
            "Borrowers retrieved successfully."));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateBorrower(
        [FromBody] CreateBorrowerRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateBorrowerCommand(
            request.FirstName,
            request.LastName,
            request.Email,
            request.PhoneNumber,
            request.DateOfBirth);

        var borrowerId = await _sender.Send(command, cancellationToken);

        return Ok(ApiResponse<Guid>.Ok(
            borrowerId,
            "Borrower created successfully."));
    }
}