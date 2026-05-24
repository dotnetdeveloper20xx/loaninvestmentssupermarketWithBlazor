using FluentValidation;

namespace LoanSuperMarket.Application.Features.Funding.FundLoan;

public sealed class FundLoanCommandValidator : AbstractValidator<FundLoanCommand>
{
    public FundLoanCommandValidator()
    {
        RuleFor(x => x.ApplicationId)
            .NotEmpty()
            .WithMessage("Application ID is required.");

        RuleFor(x => x.LenderId)
            .NotEmpty()
            .WithMessage("Lender ID is required.");
    }
}
