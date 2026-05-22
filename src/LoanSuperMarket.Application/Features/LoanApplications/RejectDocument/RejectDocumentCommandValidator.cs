using FluentValidation;

namespace LoanSuperMarket.Application.Features.LoanApplications.RejectDocument;

public sealed class RejectDocumentCommandValidator
    : AbstractValidator<RejectDocumentCommand>
{
    public RejectDocumentCommandValidator()
    {
        RuleFor(x => x.ApplicationId)
            .NotEmpty();

        RuleFor(x => x.DocumentId)
            .NotEmpty();

        RuleFor(x => x.RejectionNote)
            .NotEmpty()
            .MaximumLength(2000);
    }
}
