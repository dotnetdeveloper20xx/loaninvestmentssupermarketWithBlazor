using FluentValidation;

namespace LoanSuperMarket.Application.Features.Payments.RecordPayment;

public sealed class RecordPaymentCommandValidator : AbstractValidator<RecordPaymentCommand>
{
    public RecordPaymentCommandValidator()
    {
        RuleFor(x => x.ScheduleId)
            .NotEmpty()
            .WithMessage("Schedule ID is required.");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Payment amount must be greater than zero.");

        RuleFor(x => x.PaymentDate)
            .NotEmpty()
            .WithMessage("Payment date is required.")
            .LessThanOrEqualTo(DateTime.UtcNow.AddDays(1))
            .WithMessage("Payment date cannot be in the future.");
    }
}
