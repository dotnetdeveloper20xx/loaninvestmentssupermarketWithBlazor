using FluentValidation;

namespace LoanSuperMarket.Application.Features.LoanApplications.UpdateDraftLoanApplication;

public sealed class UpdateDraftLoanApplicationCommandValidator
    : AbstractValidator<UpdateDraftLoanApplicationCommand>
{
    public UpdateDraftLoanApplicationCommandValidator()
    {
        RuleFor(x => x.ApplicationId)
            .NotEmpty();

        RuleFor(x => x.RequestedAmount)
            .GreaterThan(0);

        RuleFor(x => x.TermMonths)
            .InclusiveBetween(1, 600);

        RuleFor(x => x.Purpose)
            .NotEmpty()
            .MaximumLength(1000);
    }
}
