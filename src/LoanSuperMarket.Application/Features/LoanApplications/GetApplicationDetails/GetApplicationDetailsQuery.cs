using LoanSuperMarket.Shared.LoanApplications;
using MediatR;

namespace LoanSuperMarket.Application.Features.LoanApplications.GetApplicationDetails;

public sealed record GetApplicationDetailsQuery(Guid ApplicationId)
    : IRequest<ApplicationDetailDto>;
