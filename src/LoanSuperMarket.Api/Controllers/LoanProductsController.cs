using LoanSuperMarket.Application.Features.LoanProducts.ApproveLoanProduct;
using LoanSuperMarket.Application.Features.LoanProducts.CreateLoanProduct;
using LoanSuperMarket.Application.Features.LoanProducts.GetLoanProductById;
using LoanSuperMarket.Application.Features.LoanProducts.GetLoanProducts;
using LoanSuperMarket.Application.Features.LoanProducts.PublishLoanProduct;
using LoanSuperMarket.Application.Features.LoanProducts.SubmitLoanProductForApproval;
using LoanSuperMarket.Shared.Common;
using LoanSuperMarket.Shared.LoanProducts;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LoanSuperMarket.Api.Controllers;

[ApiController]
[Route("api/loan-products")]
public sealed class LoanProductsController : ControllerBase
{
    private readonly ISender _sender;

    public LoanProductsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<LoanProductDto>>>> GetLoanProducts(
        CancellationToken cancellationToken)
    {
        var loanProducts = await _sender.Send(new GetLoanProductsQuery(), cancellationToken);

        return Ok(ApiResponse<IReadOnlyList<LoanProductDto>>.Ok(
            loanProducts,
            "Loan products retrieved successfully."));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateLoanProduct(
        [FromBody] CreateLoanProductRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateLoanProductCommand(
            request.Title,
            request.Description,
            request.MinimumAmount,
            request.MaximumAmount,
            request.InterestRate,
            request.MinimumTermMonths,
            request.MaximumTermMonths,
            request.LenderId);

        var loanProductId = await _sender.Send(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetLoanProducts),
            new { id = loanProductId },
            ApiResponse<Guid>.Ok(loanProductId, "Loan product created successfully."));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<LoanProductDto>>> GetLoanProductById(
    Guid id,
    CancellationToken cancellationToken)
    {
        var loanProduct = await _sender.Send(
            new GetLoanProductByIdQuery(id),
            cancellationToken);

        if (loanProduct is null)
        {
            return NotFound(ApiResponse<LoanProductDto>.Fail("Loan product was not found."));
        }

        return Ok(ApiResponse<LoanProductDto>.Ok(
            loanProduct,
            "Loan product retrieved successfully."));
    }

    [HttpPost("{id:guid}/submit-for-approval")]
    public async Task<ActionResult<ApiResponse<string>>> SubmitForApproval(
    Guid id,
    CancellationToken cancellationToken)
    {
        await _sender.Send(
            new SubmitLoanProductForApprovalCommand(id),
            cancellationToken);

        return Ok(ApiResponse<string>.Ok(
            "Loan product submitted for approval.",
            "Workflow action completed."));
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult<ApiResponse<string>>> Approve(
    Guid id,
    CancellationToken cancellationToken)
    {
        await _sender.Send(
            new ApproveLoanProductCommand(id),
            cancellationToken);

        return Ok(ApiResponse<string>.Ok(
            "Loan product approved.",
            "Workflow action completed."));
    }

    [HttpPost("{id:guid}/publish")]
    public async Task<ActionResult<ApiResponse<string>>> Publish(
    Guid id,
    CancellationToken cancellationToken)
    {
        await _sender.Send(
            new PublishLoanProductCommand(id),
            cancellationToken);

        return Ok(ApiResponse<string>.Ok(
            "Loan product published.",
            "Workflow action completed."));
    }
}