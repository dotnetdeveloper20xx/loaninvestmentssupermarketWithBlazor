using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Domain.Common;
using LoanSuperMarket.Domain.Enums;
using MediatR;

namespace LoanSuperMarket.Application.Features.LoanApplications.SubmitLoanApplication;

public sealed class SubmitLoanApplicationCommandHandler
    : IRequestHandler<SubmitLoanApplicationCommand>
{
    private readonly ILoanApplicationRepository _repository;
    private readonly IApplicationDocumentRepository _documentRepository;

    public SubmitLoanApplicationCommandHandler(
        ILoanApplicationRepository repository,
        IApplicationDocumentRepository documentRepository)
    {
        _repository = repository;
        _documentRepository = documentRepository;
    }

    public async Task Handle(
        SubmitLoanApplicationCommand request,
        CancellationToken cancellationToken)
    {
        var application = await _repository.GetByIdAsync(request.ApplicationId, cancellationToken)
            ?? throw new DomainException("Loan application was not found.");

        var documents = await _documentRepository.GetByApplicationIdAsync(
            request.ApplicationId, cancellationToken);

        var missingDocuments = new List<string>();

        if (!documents.Any(d => d.Type == DocumentType.NationalID))
            missingDocuments.Add("National ID");

        if (!documents.Any(d => d.Type == DocumentType.ProofOfIncome))
            missingDocuments.Add("Proof of Income");

        if (!documents.Any(d => d.Type == DocumentType.BankStatement))
            missingDocuments.Add("Bank Statement");

        if (missingDocuments.Count > 0)
        {
            throw new DomainException(
                $"Cannot submit application. Missing required documents: {string.Join(", ", missingDocuments)}.");
        }

        application.Submit();

        await _repository.SaveChangesAsync(cancellationToken);
    }
}
