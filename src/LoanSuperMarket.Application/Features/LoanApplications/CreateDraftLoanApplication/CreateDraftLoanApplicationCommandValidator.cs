using FluentValidation;

namespace LoanSuperMarket.Application.Features.LoanApplications.CreateDraftLoanApplication;

public sealed class CreateDraftLoanApplicationCommandValidator
    : AbstractValidator<CreateDraftLoanApplicationCommand>
{
    public CreateDraftLoanApplicationCommandValidator()
    {
        RuleFor(x => x.RequestedAmount)
            .GreaterThan(0);

        RuleFor(x => x.TermMonths)
            .InclusiveBetween(1, 600);

        RuleFor(x => x.Purpose)
            .NotEmpty()
            .MaximumLength(1000);
    }
}
