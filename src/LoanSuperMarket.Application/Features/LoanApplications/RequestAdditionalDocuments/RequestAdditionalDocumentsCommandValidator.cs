using FluentValidation;

namespace LoanSuperMarket.Application.Features.LoanApplications.RequestAdditionalDocuments;

public sealed class RequestAdditionalDocumentsCommandValidator
    : AbstractValidator<RequestAdditionalDocumentsCommand>
{
    public RequestAdditionalDocumentsCommandValidator()
    {
        RuleFor(x => x.ApplicationId)
            .NotEmpty();

        RuleFor(x => x.Note)
            .NotEmpty()
            .MaximumLength(2000);
    }
}
