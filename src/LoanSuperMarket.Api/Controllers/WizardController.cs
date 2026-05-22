using LoanSuperMarket.Application.Features.LoanApplications.CreateDraftLoanApplication;
using LoanSuperMarket.Application.Features.LoanApplications.GetApplicationDocuments;
using LoanSuperMarket.Application.Features.LoanApplications.GetBorrowerApplications;
using LoanSuperMarket.Application.Features.LoanApplications.MatchProducts;
using LoanSuperMarket.Application.Features.LoanApplications.RemoveDocument;
using LoanSuperMarket.Application.Features.LoanApplications.ResubmitForReview;
using LoanSuperMarket.Application.Features.LoanApplications.SelectProduct;
using LoanSuperMarket.Application.Features.LoanApplications.SubmitLoanApplication;
using LoanSuperMarket.Application.Features.LoanApplications.UpdateDraftLoanApplication;
using LoanSuperMarket.Application.Features.LoanApplications.UploadDocument;
using LoanSuperMarket.Application.Features.LoanApplications.WithdrawLoanApplication;
using LoanSuperMarket.Domain.Enums;
using LoanSuperMarket.Shared.Common;
using LoanSuperMarket.Shared.LoanApplications;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LoanSuperMarket.Api.Controllers;

[ApiController]
[Route("api/wizard")]
[Authorize(Roles = "Borrower")]
public sealed class WizardController : ControllerBase
{
    private readonly ISender _sender;

    public WizardController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost("create-draft")]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateDraft(
        [FromBody] CreateDraftRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateDraftLoanApplicationCommand(
            request.RequestedAmount,
            request.TermMonths,
            request.Purpose);

        var applicationId = await _sender.Send(command, cancellationToken);

        return Ok(ApiResponse<Guid>.Ok(
            applicationId,
            "Draft loan application created successfully."));
    }

    [HttpPut("{id:guid}/parameters")]
    public async Task<ActionResult<ApiResponse<string>>> UpdateParameters(
        Guid id,
        [FromBody] UpdateDraftParametersRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateDraftLoanApplicationCommand(
            id,
            request.RequestedAmount,
            request.TermMonths,
            request.Purpose);

        await _sender.Send(command, cancellationToken);

        return Ok(ApiResponse<string>.Ok(
            "Draft parameters updated.",
            "Parameters updated successfully."));
    }

    [HttpPost("{id:guid}/match-products")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<MatchedProductDto>>>> MatchProducts(
        Guid id,
        CancellationToken cancellationToken)
    {
        var query = new MatchProductsQuery(id);
        var products = await _sender.Send(query, cancellationToken);

        return Ok(ApiResponse<IReadOnlyList<MatchedProductDto>>.Ok(
            products,
            "Matched products retrieved successfully."));
    }

    [HttpPut("{id:guid}/select-product")]
    public async Task<ActionResult<ApiResponse<string>>> SelectProduct(
        Guid id,
        [FromBody] SelectProductRequest request,
        CancellationToken cancellationToken)
    {
        var command = new SelectProductCommand(id, request.LoanProductId);
        await _sender.Send(command, cancellationToken);

        return Ok(ApiResponse<string>.Ok(
            "Product selected.",
            "Product associated with application successfully."));
    }

    [HttpPost("{id:guid}/documents")]
    public async Task<ActionResult<ApiResponse<Guid>>> UploadDocument(
        Guid id,
        [FromForm] IFormFile file,
        [FromForm] int documentType,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(ApiResponse<Guid>.Fail("File is required."));
        }

        if (!Enum.IsDefined(typeof(DocumentType), documentType))
        {
            return BadRequest(ApiResponse<Guid>.Fail("Invalid document type."));
        }

        await using var stream = file.OpenReadStream();

        var command = new UploadDocumentCommand(
            id,
            (DocumentType)documentType,
            file.FileName,
            stream);

        var documentId = await _sender.Send(command, cancellationToken);

        return Ok(ApiResponse<Guid>.Ok(
            documentId,
            "Document uploaded successfully."));
    }

    [HttpDelete("{id:guid}/documents/{docId:guid}")]
    public async Task<ActionResult<ApiResponse<string>>> RemoveDocument(
        Guid id,
        Guid docId,
        CancellationToken cancellationToken)
    {
        var command = new RemoveDocumentCommand(id, docId);
        await _sender.Send(command, cancellationToken);

        return Ok(ApiResponse<string>.Ok(
            "Document removed.",
            "Document removed successfully."));
    }

    [HttpGet("{id:guid}/documents")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ApplicationDocumentDto>>>> GetDocuments(
        Guid id,
        CancellationToken cancellationToken)
    {
        var query = new GetApplicationDocumentsQuery(id);
        var documents = await _sender.Send(query, cancellationToken);

        return Ok(ApiResponse<IReadOnlyList<ApplicationDocumentDto>>.Ok(
            documents,
            "Documents retrieved successfully."));
    }

    [HttpPost("{id:guid}/submit")]
    public async Task<ActionResult<ApiResponse<string>>> Submit(
        Guid id,
        CancellationToken cancellationToken)
    {
        var command = new SubmitLoanApplicationCommand(id);
        await _sender.Send(command, cancellationToken);

        return Ok(ApiResponse<string>.Ok(
            "Application submitted.",
            "Loan application submitted successfully."));
    }

    [HttpPost("{id:guid}/withdraw")]
    public async Task<ActionResult<ApiResponse<string>>> Withdraw(
        Guid id,
        CancellationToken cancellationToken)
    {
        var command = new WithdrawLoanApplicationCommand(id);
        await _sender.Send(command, cancellationToken);

        return Ok(ApiResponse<string>.Ok(
            "Application withdrawn.",
            "Loan application withdrawn successfully."));
    }

    [HttpPost("{id:guid}/resubmit")]
    public async Task<ActionResult<ApiResponse<string>>> Resubmit(
        Guid id,
        CancellationToken cancellationToken)
    {
        var command = new ResubmitForReviewCommand(id);
        await _sender.Send(command, cancellationToken);

        return Ok(ApiResponse<string>.Ok(
            "Application resubmitted.",
            "Loan application resubmitted for review."));
    }

    [HttpGet("/api/borrower/applications")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<WizardApplicationSummaryDto>>>> GetBorrowerApplications(
        CancellationToken cancellationToken)
    {
        var query = new GetBorrowerApplicationsQuery();
        var applications = await _sender.Send(query, cancellationToken);

        return Ok(ApiResponse<IReadOnlyList<WizardApplicationSummaryDto>>.Ok(
            applications,
            "Borrower applications retrieved successfully."));
    }
}
