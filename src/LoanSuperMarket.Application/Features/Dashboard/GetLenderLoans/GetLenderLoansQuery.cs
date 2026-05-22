using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Shared.Common;
using LoanSuperMarket.Shared.Dashboard;
using MediatR;

namespace LoanSuperMarket.Application.Features.Dashboard.GetLenderLoans;

public sealed class GetLenderLoansQuery : IRequest<ApiResponse<IReadOnlyList<LenderLoanDto>>>, IResourceFilteredQuery
{
    public string? PerformanceFilter { get; set; }

    public string? SortBy { get; set; }

    public string? FilterByUserId { get; set; }

    public string? FilterByRole { get; set; }
}
