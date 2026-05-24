using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Application.Features.Dashboard;
using LoanSuperMarket.Application.Features.Dashboard.GetBorrowerLoans;
using LoanSuperMarket.Application.Features.Dashboard.GetBorrowerPaymentSummary;
using LoanSuperMarket.Application.Features.Dashboard.GetLenderDashboard;
using LoanSuperMarket.Application.Features.Dashboard.GetLenderEarnings;
using LoanSuperMarket.Application.Features.Dashboard.GetLenderLoans;
using LoanSuperMarket.Shared.Audit;
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

    [HttpGet("lender/portfolio")]
    public async Task<ActionResult<ApiResponse<LenderPortfolioDto>>> GetLenderPortfolio(
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetLenderDashboardQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("lender/loans")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<LenderLoanDto>>>> GetLenderLoans(
        [FromQuery] string? performance,
        [FromQuery] string? sortBy,
        CancellationToken cancellationToken)
    {
        var query = new GetLenderLoansQuery
        {
            PerformanceFilter = performance,
            SortBy = sortBy
        };

        var result = await _sender.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("lender/earnings")]
    public async Task<ActionResult<ApiResponse<LenderEarningsDto>>> GetLenderEarnings(
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetLenderEarningsQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("lender/analytics")]
    public async Task<ActionResult<ApiResponse<InvestorAnalyticsDto>>> GetInvestorAnalytics(
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new Application.Features.Dashboard.GetInvestorAnalytics.GetInvestorAnalyticsQuery(),
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("borrower/loans")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<BorrowerLoanDto>>>> GetBorrowerLoans(
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetBorrowerLoansQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("borrower/upcoming")]
    public async Task<ActionResult<ApiResponse<BorrowerPaymentSummaryDto>>> GetBorrowerPaymentSummary(
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetBorrowerPaymentSummaryQuery(), cancellationToken);
        return Ok(result);
    }

    [HttpGet("audit/{entityName}/{entityId:guid}")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AuditLogDto>>>> GetAuditTrail(
        string entityName,
        Guid entityId,
        CancellationToken cancellationToken)
    {
        var auditLogRepository = HttpContext.RequestServices.GetRequiredService<IAuditLogRepository>();
        var logs = await auditLogRepository.GetByEntityAsync(entityName, entityId, cancellationToken);

        return Ok(ApiResponse<IReadOnlyList<AuditLogDto>>.Ok(
            logs,
            "Audit trail retrieved successfully."));
    }

    [HttpGet("admin/loans")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<AdminLoansOverviewDto>>> GetAdminLoansOverview(
        [FromQuery] string? performance,
        [FromQuery] string? lender,
        CancellationToken cancellationToken)
    {
        var query = new Application.Features.Dashboard.GetAdminLoansOverview.GetAdminLoansOverviewQuery
        {
            PerformanceFilter = performance,
            LenderFilter = lender
        };

        var result = await _sender.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("admin/collections")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CollectionItemDto>>>> GetCollections(
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new Application.Features.Dashboard.GetCollections.GetCollectionsQuery(),
            cancellationToken);
        return Ok(result);
    }
}