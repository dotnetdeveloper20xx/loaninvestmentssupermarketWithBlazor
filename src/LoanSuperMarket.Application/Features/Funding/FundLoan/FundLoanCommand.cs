using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Shared.Common;
using LoanSuperMarket.Shared.Funding;
using MediatR;

namespace LoanSuperMarket.Application.Features.Funding.FundLoan;

public sealed record FundLoanCommand(
    Guid ApplicationId,
    Guid LenderId) : IRequest<ApiResponse<FundingResultDto>>, ILoanFundingCommand
{
    // ILoanFundingCommand.Amount is resolved at handler time from the application
    public decimal Amount => 0; // Placeholder — actual enforcement uses loaded application amount
}
