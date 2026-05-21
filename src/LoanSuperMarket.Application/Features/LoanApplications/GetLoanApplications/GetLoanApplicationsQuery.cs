using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Shared.LoanApplications;
using MediatR;

namespace LoanSuperMarket.Application.Features.LoanApplications.GetLoanApplications;

public sealed record GetLoanApplicationsQuery : IRequest<IReadOnlyList<LoanApplicationDto>>, IResourceFilteredQuery
{
    /// <inheritdoc />
    public string? FilterByUserId { get; set; }

    /// <inheritdoc />
    public string? FilterByRole { get; set; }
}