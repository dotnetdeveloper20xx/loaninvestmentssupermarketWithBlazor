using LoanSuperMarket.Application.Features.LoanApplications.ApproveLoanApplication;
using LoanSuperMarket.Application.Features.LoanApplications.GetApplicationDetails;
using LoanSuperMarket.Application.Features.LoanApplications.GetReviewQueue;
using LoanSuperMarket.Application.Features.LoanApplications.MarkLoanApplicationUnderReview;
using LoanSuperMarket.Application.Features.LoanApplications.RejectDocument;
using LoanSuperMarket.Application.Features.LoanApplications.RejectLoanApplication;
using LoanSuperMarket.Application.Features.LoanApplications.RequestAdditionalDocuments;
using LoanSuperMarket.Application.Features.LoanApplications.VerifyDocument;
using LoanSuperMarket.Shared.Common;
using LoanSuperMarket.Shared.LoanApplications;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LoanSuperMarket.Api.Controllers;

[ApiController]
[Route("api/review-queue")]
[Authorize(Policy = "CanProcessApplications")]
public sealed class ReviewQueueController : ControllerBase
{
    private readonly ISender _sender;

    public ReviewQueueController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ReviewQueueItemDto>>>> GetReviewQueue(
        [FromQuery] int? statusFilter,
        [FromQuery] string? sortBy,
        CancellationToken cancellationToken)
    {
        var query = new GetReviewQueueQuery(statusFilter, sortBy);
        var items = await _sender.Send(query, cancellationToken);

        return Ok(ApiResponse<IReadOnlyList<ReviewQueueItemDto>>.Ok(
            items,
            "Review queue retrieved successfully."));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ApplicationDetailDto>>> GetApplicationDetails(
        Guid id,
        CancellationToken cancellationToken)
    {
        var query = new GetApplicationDetailsQuery(id);
        var details = await _sender.Send(query, cancellationToken);

        return Ok(ApiResponse<ApplicationDetailDto>.Ok(
            details,
            "Application details retrieved successfully."));
    }

    [HttpPost("{id:guid}/mark-under-review")]
    public async Task<ActionResult<ApiResponse<string>>> MarkUnderReview(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _sender.Send(new MarkLoanApplicationUnderReviewCommand(id), cancellationToken);

        return Ok(ApiResponse<string>.Ok(
            "Application moved under review.",
            "Workflow action completed."));
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult<ApiResponse<string>>> Approve(
        Guid id,
        [FromBody] ApproveRejectRequest request,
        CancellationToken cancellationToken)
    {
        await _sender.Send(new ApproveLoanApplicationCommand(id, request.Reason), cancellationToken);

        return Ok(ApiResponse<string>.Ok(
            "Application approved.",
            "Workflow action completed."));
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<ActionResult<ApiResponse<string>>> Reject(
        Guid id,
        [FromBody] ApproveRejectRequest request,
        CancellationToken cancellationToken)
    {
        await _sender.Send(new RejectLoanApplicationCommand(id, request.Reason), cancellationToken);

        return Ok(ApiResponse<string>.Ok(
            "Application rejected.",
            "Workflow action completed."));
    }

    [HttpPost("{id:guid}/request-documents")]
    public async Task<ActionResult<ApiResponse<string>>> RequestDocuments(
        Guid id,
        [FromBody] RequestDocumentsRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RequestAdditionalDocumentsCommand(id, request.Note);
        await _sender.Send(command, cancellationToken);

        return Ok(ApiResponse<string>.Ok(
            "Additional documents requested.",
            "Workflow action completed."));
    }

    [HttpPost("{id:guid}/documents/{docId:guid}/verify")]
    public async Task<ActionResult<ApiResponse<string>>> VerifyDocument(
        Guid id,
        Guid docId,
        CancellationToken cancellationToken)
    {
        var command = new VerifyDocumentCommand(id, docId);
        await _sender.Send(command, cancellationToken);

        return Ok(ApiResponse<string>.Ok(
            "Document verified.",
            "Document verification completed."));
    }

    [HttpPost("{id:guid}/documents/{docId:guid}/reject")]
    public async Task<ActionResult<ApiResponse<string>>> RejectDocument(
        Guid id,
        Guid docId,
        [FromBody] RejectDocumentRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RejectDocumentCommand(id, docId, request.RejectionNote);
        await _sender.Send(command, cancellationToken);

        return Ok(ApiResponse<string>.Ok(
            "Document rejected.",
            "Document rejection completed."));
    }
}
