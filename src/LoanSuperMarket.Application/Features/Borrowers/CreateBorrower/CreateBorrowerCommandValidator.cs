using FluentValidation;

namespace LoanSuperMarket.Application.Features.Borrowers.CreateBorrower;

public sealed class CreateBorrowerCommandValidator : AbstractValidator<CreateBorrowerCommand>
{
    public CreateBorrowerCommandValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.LastName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(250);

        RuleFor(x => x.PhoneNumber)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.DateOfBirth)
            .LessThanOrEqualTo(DateTime.Today.AddYears(-18))
            .WithMessage("Borrower must be at least 18 years old.");
    }
}