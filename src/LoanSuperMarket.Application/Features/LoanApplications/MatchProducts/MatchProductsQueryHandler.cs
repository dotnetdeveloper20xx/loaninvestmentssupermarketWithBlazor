using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Application.Features.LoanApplications.ProductMatching;
using LoanSuperMarket.Domain.Common;
using LoanSuperMarket.Domain.Enums;
using LoanSuperMarket.Shared.LoanApplications;
using MediatR;

namespace LoanSuperMarket.Application.Features.LoanApplications.MatchProducts;

public sealed class MatchProductsQueryHandler
    : IRequestHandler<MatchProductsQuery, IReadOnlyList<MatchedProductDto>>
{
    private readonly ILoanApplicationRepository _applicationRepository;
    private readonly IBorrowerRepository _borrowerRepository;
    private readonly IIdentityService _identityService;
    private readonly ProductMatchingService _productMatchingService;

    public MatchProductsQueryHandler(
        ILoanApplicationRepository applicationRepository,
        IBorrowerRepository borrowerRepository,
        IIdentityService identityService,
        ProductMatchingService productMatchingService)
    {
        _applicationRepository = applicationRepository;
        _borrowerRepository = borrowerRepository;
        _identityService = identityService;
        _productMatchingService = productMatchingService;
    }

    public async Task<IReadOnlyList<MatchedProductDto>> Handle(
        MatchProductsQuery request,
        CancellationToken cancellationToken)
    {
        var application = await _applicationRepository.GetByIdAsync(request.ApplicationId, cancellationToken)
            ?? throw new DomainException("Loan application was not found.");

        var borrower = await _borrowerRepository.GetByIdAsync(application.BorrowerId, cancellationToken)
            ?? throw new DomainException("Borrower was not found.");

        // Resolve credit tier from the ApplicationUser linked to the borrower
        var creditTier = CreditTier.C; // Default to worst tier if not set

        if (borrower.UserId is not null)
        {
            var user = await _identityService.GetUserByIdAsync(borrower.UserId, cancellationToken);
            if (user?.CreditTier is not null)
            {
                creditTier = user.CreditTier.Value;
            }
        }

        return await _productMatchingService.MatchProductsAsync(
            application.RequestedAmount.Amount,
            application.TermMonths,
            creditTier,
            cancellationToken);
    }
}
