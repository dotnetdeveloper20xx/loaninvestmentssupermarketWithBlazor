using FluentValidation;

namespace LoanSuperMarket.Application.Features.Lenders.CreateLender;

public sealed class CreateLenderCommandValidator : AbstractValidator<CreateLenderCommand>
{
    public CreateLenderCommandValidator()
    {
        RuleFor(x => x.CompanyName)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.ContactName)
            .NotEmpty()
            .MaximumLength(150);

        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(250);

        RuleFor(x => x.PhoneNumber)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.AvailableFunds)
            .GreaterThanOrEqualTo(0);
    }
}