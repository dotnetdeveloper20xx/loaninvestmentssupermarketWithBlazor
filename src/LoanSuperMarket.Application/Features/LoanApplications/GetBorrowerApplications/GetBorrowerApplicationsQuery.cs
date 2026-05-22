using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Shared.LoanApplications;
using MediatR;

namespace LoanSuperMarket.Application.Features.LoanApplications.GetBorrowerApplications;

public sealed record GetBorrowerApplicationsQuery
    : IRequest<IReadOnlyList<WizardApplicationSummaryDto>>, IResourceFilteredQuery
{
    /// <inheritdoc />
    public string? FilterByUserId { get; set; }

    /// <inheritdoc />
    public string? FilterByRole { get; set; }
}
