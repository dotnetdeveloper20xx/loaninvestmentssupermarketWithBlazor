using LoanSuperMarket.Application.Features.Dashboard;
using LoanSuperMarket.Shared.Common;
using LoanSuperMarket.Shared.Dashboard;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LoanSuperMarket.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize(Policy = "CanViewReports")]
public sealed class DashboardController : ControllerBase
{
    private readonly ISender _sender;

    public DashboardController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<ApiResponse<DashboardSummaryDto>>> GetSummary(
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new GetDashboardSummaryQuery(),
            cancellationToken);

        return Ok(ApiResponse<DashboardSummaryDto>.Ok(
            result,
            "Dashboard summary retrieved successfully."));
    }
}