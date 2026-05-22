using FluentValidation;

namespace LoanSuperMarket.Application.Features.LoanApplications.UploadDocument;

public sealed class UploadDocumentCommandValidator
    : AbstractValidator<UploadDocumentCommand>
{
    public UploadDocumentCommandValidator()
    {
        RuleFor(x => x.ApplicationId)
            .NotEmpty();

        RuleFor(x => x.DocumentType)
            .IsInEnum();

        RuleFor(x => x.FileName)
            .NotEmpty()
            .MaximumLength(500);

        RuleFor(x => x.FileStream)
            .NotNull()
            .WithMessage("File is required.");
    }
}
