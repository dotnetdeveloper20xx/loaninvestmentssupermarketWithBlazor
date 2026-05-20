using FluentValidation;

namespace LoanSuperMarket.Application.Features.LoanApplications.CreateLoanApplication;

public sealed class CreateLoanApplicationCommandValidator : AbstractValidator<CreateLoanApplicationCommand>
{
    public CreateLoanApplicationCommandValidator()
    {
        RuleFor(x => x.BorrowerId)
            .NotEmpty();

        RuleFor(x => x.LoanProductId)
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