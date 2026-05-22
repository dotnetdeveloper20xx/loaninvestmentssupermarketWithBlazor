using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Shared.Common;
using LoanSuperMarket.Shared.Dashboard;
using MediatR;

namespace LoanSuperMarket.Application.Features.Dashboard.GetBorrowerLoans;

public sealed class GetBorrowerLoansQuery : IRequest<ApiResponse<IReadOnlyList<BorrowerLoanDto>>>, IResourceFilteredQuery
{
    public string? FilterByUserId { get; set; }

    public string? FilterByRole { get; set; }
}
