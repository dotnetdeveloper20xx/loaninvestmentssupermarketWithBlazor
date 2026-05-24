using FluentValidation;

namespace LoanSuperMarket.Application.Features.Funding.TopUpFunds;

public sealed class TopUpFundsCommandValidator : AbstractValidator<TopUpFundsCommand>
{
    public TopUpFundsCommandValidator()
    {
        RuleFor(x => x.LenderId)
            .NotEmpty()
            .WithMessage("Lender ID is required.");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Top-up amount must be greater than zero.")
            .LessThanOrEqualTo(10_000_000m)
            .WithMessage("Top-up amount cannot exceed £10,000,000.");
    }
}
