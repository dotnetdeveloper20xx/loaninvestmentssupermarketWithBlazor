using LoanSuperMarket.Shared.LoanApplications;
using MediatR;

namespace LoanSuperMarket.Application.Features.LoanApplications.GetLoanApplications;

public sealed record GetLoanApplicationsQuery : IRequest<IReadOnlyList<LoanApplicationDto>>;