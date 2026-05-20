using LoanSuperMarket.Application.Features.LoanApplications.CreateLoanApplication;
using LoanSuperMarket.Application.Features.LoanApplications.GetLoanApplications;
using LoanSuperMarket.Shared.Common;
using LoanSuperMarket.Shared.LoanApplications;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LoanSuperMarket.Api.Controllers;

[ApiController]
[Route("api/loan-applications")]
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
}