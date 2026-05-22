using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Shared.Common;
using LoanSuperMarket.Shared.Dashboard;
using MediatR;

namespace LoanSuperMarket.Application.Features.Dashboard.GetBorrowerPaymentSummary;

public sealed class GetBorrowerPaymentSummaryQuery : IRequest<ApiResponse<BorrowerPaymentSummaryDto>>, IResourceFilteredQuery
{
    public string? FilterByUserId { get; set; }

    public string? FilterByRole { get; set; }
}
