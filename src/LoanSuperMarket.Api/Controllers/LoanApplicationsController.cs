using LoanSuperMarket.Application.Features.LoanApplications.CreateLoanApplication;
using LoanSuperMarket.Application.Features.LoanApplications.GetLoanApplications;
using LoanSuperMarket.Application.Features.LoanApplications.ApproveLoanApplication;
using LoanSuperMarket.Application.Features.LoanApplications.FundLoanApplication;
using LoanSuperMarket.Application.Features.LoanApplications.MarkLoanApplicationUnderReview;
using LoanSuperMarket.Application.Features.LoanApplications.RejectLoanApplication;
using LoanSuperMarket.Shared.Common;
using LoanSuperMarket.Shared.LoanApplications;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LoanSuperMarket.Api.Controllers;

[ApiController]
[Route("api/loan-applications")]
[Authorize(Policy = "CanProcessApplications")]
public sealed class LoanApplicationsController : ControllerBase
{
    private readonly ISender _sender;

    public LoanApplicationsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<LoanApplicationDto>>>> GetLoanApplications(
        CancellationToken cancellationToken)
    {
        var applications = await _sender.Send(
            new GetLoanApplicationsQuery(),
            cancellationToken);

        return Ok(ApiResponse<IReadOnlyList<LoanApplicationDto>>.Ok(
            applications,
            "Loan applications retrieved successfully."));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<Guid>>> CreateLoanApplication(
        [FromBody] CreateLoanApplicationRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateLoanApplicationCommand(
            request.BorrowerId,
            request.LoanProductId,
            request.RequestedAmount,
            request.TermMonths,
            request.Purpose);

        var applicationId = await _sender.Send(command, cancellationToken);

        return Ok(ApiResponse<Guid>.Ok(
            applicationId,
            "Loan application submitted successfully."));
    }

    [HttpPost("{id:guid}/mark-under-review")]
    public async Task<ActionResult<ApiResponse<string>>> MarkUnderReview(
    Guid id,
    CancellationToken cancellationToken)
    {
        await _sender.Send(new MarkLoanApplicationUnderReviewCommand(id), cancellationToken);

        return Ok(ApiResponse<string>.Ok(
            "Loan application moved under review.",
            "Workflow action completed."));
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult<ApiResponse<string>>> Approve(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _sender.Send(new ApproveLoanApplicationCommand(id), cancellationToken);

        return Ok(ApiResponse<string>.Ok(
            "Loan application approved.",
            "Workflow action completed."));
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<ActionResult<ApiResponse<string>>> Reject(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _sender.Send(new RejectLoanApplicationCommand(id), cancellationToken);

        return Ok(ApiResponse<string>.Ok(
            "Loan application rejected.",
            "Workflow action completed."));
    }

    [HttpPost("{id:guid}/fund")]
    public async Task<ActionResult<ApiResponse<string>>> Fund(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _sender.Send(new FundLoanApplicationCommand(id), cancellationToken);

        return Ok(ApiResponse<string>.Ok(
            "Loan application funded.",
            "Workflow action completed."));
    }
}