using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Domain.Common;
using LoanSuperMarket.Domain.Enums;
using LoanSuperMarket.Shared.LoanApplications;
using MediatR;

namespace LoanSuperMarket.Application.Features.LoanApplications.GetApplicationDetails;

public sealed class GetApplicationDetailsQueryHandler
    : IRequestHandler<GetApplicationDetailsQuery, ApplicationDetailDto>
{
    private readonly ILoanApplicationRepository _applicationRepository;
    private readonly IBorrowerRepository _borrowerRepository;
    private readonly ILoanProductRepository _productRepository;
    private readonly IApplicationDocumentRepository _documentRepository;
    private readonly IIdentityService _identityService;

    public GetApplicationDetailsQueryHandler(
        ILoanApplicationRepository applicationRepository,
        IBorrowerRepository borrowerRepository,
        ILoanProductRepository productRepository,
        IApplicationDocumentRepository documentRepository,
        IIdentityService identityService)
    {
        _applicationRepository = applicationRepository;
        _borrowerRepository = borrowerRepository;
        _productRepository = productRepository;
        _documentRepository = documentRepository;
        _identityService = identityService;
    }

    public async Task<ApplicationDetailDto> Handle(
        GetApplicationDetailsQuery request,
        CancellationToken cancellationToken)
    {
        var application = await _applicationRepository.GetByIdAsync(request.ApplicationId, cancellationToken)
            ?? throw new DomainException("Loan application was not found.");

        var borrower = await _borrowerRepository.GetByIdAsync(application.BorrowerId, cancellationToken)
            ?? throw new DomainException("Borrower was not found.");

        string? productTitle = null;
        if (application.LoanProductId.HasValue)
        {
            var product = await _productRepository.GetByIdAsync(application.LoanProductId.Value, cancellationToken);
            productTitle = product?.Title;
        }

        var documents = await _documentRepository.GetByApplicationIdAsync(request.ApplicationId, cancellationToken);

        var creditTier = 0;
        if (borrower.UserId is not null)
        {
            var user = await _identityService.GetUserByIdAsync(borrower.UserId, cancellationToken);
            if (user?.CreditTier is not null)
            {
                creditTier = (int)user.CreditTier.Value;
            }
        }

        return new ApplicationDetailDto
        {
            ApplicationId = application.Id,
            BorrowerName = borrower.FullName,
            BorrowerEmail = borrower.Email,
            CreditTier = creditTier,
            ProductTitle = productTitle,
            RequestedAmount = application.RequestedAmount.Amount,
            TermMonths = application.TermMonths,
            Purpose = application.Purpose,
            Status = (int)application.Status,
            SubmittedAtUtc = application.SubmittedAtUtc,
            ReviewedBy = application.ReviewedBy,
            ReviewReason = application.ReviewReason,
            ReviewedAtUtc = application.ReviewedAtUtc,
            DocumentRequestNote = application.DocumentRequestNote,
            Documents = documents.Select(d => new ApplicationDocumentDto(
                d.Id,
                d.FileName,
                (int)d.Type,
                (int)d.Status,
                d.UploadedAtUtc,
                d.VerifiedBy,
                d.VerifiedAtUtc,
                d.RejectionNote)).ToList()
        };
    }
}
