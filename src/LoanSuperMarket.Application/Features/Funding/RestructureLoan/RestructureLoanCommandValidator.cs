using FluentValidation;

namespace LoanSuperMarket.Application.Features.Funding.RestructureLoan;

public sealed class RestructureLoanCommandValidator : AbstractValidator<RestructureLoanCommand>
{
    public RestructureLoanCommandValidator()
    {
        RuleFor(x => x.ScheduleId)
            .NotEmpty()
            .WithMessage("Schedule ID is required.");

        RuleFor(x => x.NewAnnualRate)
            .GreaterThan(0)
            .WithMessage("New annual rate must be greater than zero.")
            .LessThanOrEqualTo(100)
            .WithMessage("New annual rate cannot exceed 100%.");

        RuleFor(x => x.NewTermMonths)
            .GreaterThan(0)
            .WithMessage("New term must be at least 1 month.")
            .LessThanOrEqualTo(360)
            .WithMessage("New term cannot exceed 360 months.");
    }
}
