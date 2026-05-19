using FluentValidation;

namespace LoanSuperMarket.Application.Features.LoanProducts.CreateLoanProduct;

public sealed class CreateLoanProductCommandValidator : AbstractValidator<CreateLoanProductCommand>
{
    public CreateLoanProductCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(x => x.Description)
            .NotEmpty()
            .MaximumLength(2000);

        RuleFor(x => x.MinimumAmount)
            .GreaterThan(0);

        RuleFor(x => x.MaximumAmount)
            .GreaterThan(0)
            .GreaterThanOrEqualTo(x => x.MinimumAmount);

        RuleFor(x => x.InterestRate)
            .GreaterThan(0)
            .LessThanOrEqualTo(100);

        RuleFor(x => x.MinimumTermMonths)
            .GreaterThan(0);

        RuleFor(x => x.MaximumTermMonths)
            .GreaterThan(0)
            .GreaterThanOrEqualTo(x => x.MinimumTermMonths);

        RuleFor(x => x.LenderId)
            .NotEmpty();
    }
}