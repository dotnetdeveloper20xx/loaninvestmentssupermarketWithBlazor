using FluentValidation;

namespace LoanSuperMarket.Application.Features.Funding.DeclineFunding;

public sealed class DeclineFundingCommandValidator : AbstractValidator<DeclineFundingCommand>
{
    public DeclineFundingCommandValidator()
    {
        RuleFor(x => x.ApplicationId)
            .NotEmpty()
            .WithMessage("Application ID is required.");

        RuleFor(x => x.LenderId)
            .NotEmpty()
            .WithMessage("Lender ID is required.");

        RuleFor(x => x.DeclineReason)
            .NotEmpty()
            .WithMessage("A decline reason is required.")
            .MaximumLength(1000)
            .WithMessage("Decline reason cannot exceed 1000 characters.");
    }
}
