using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Domain.Enums;
using LoanSuperMarket.Shared.LoanApplications;

namespace LoanSuperMarket.Application.Features.LoanApplications.ProductMatching;

public sealed class ProductMatchingService
{
    private readonly ILoanProductRepository _productRepository;
    private readonly ILenderRepository _lenderRepository;

    public ProductMatchingService(
        ILoanProductRepository productRepository,
        ILenderRepository lenderRepository)
    {
        _productRepository = productRepository;
        _lenderRepository = lenderRepository;
    }

    public async Task<IReadOnlyList<MatchedProductDto>> MatchProductsAsync(
        decimal requestedAmount,
        int requestedTermMonths,
        CreditTier borrowerTier,
        CancellationToken ct)
    {
        var publishedProducts = await _productRepository.GetPublishedAsync(ct);

        var lenders = await _lenderRepository.GetAllAsync(ct);
        var lenderLookup = lenders.ToDictionary(l => l.Id, l => l.CompanyName);

        var matched = publishedProducts
            .Where(p => p.MinimumAmount.Amount <= requestedAmount && requestedAmount <= p.MaximumAmount.Amount)
            .Where(p => p.MinimumTermMonths <= requestedTermMonths && requestedTermMonths <= p.MaximumTermMonths)
            .Select(p => new MatchedProductDto(
                ProductId: p.Id,
                Title: p.Title,
                LenderName: lenderLookup.TryGetValue(p.LenderId, out var name) ? name : "Unknown",
                EffectiveInterestRate: CalculateEffectiveRate(p.InterestRate.Percentage, borrowerTier),
                MinimumAmount: p.MinimumAmount.Amount,
                MaximumAmount: p.MaximumAmount.Amount,
                MinimumTermMonths: p.MinimumTermMonths,
                MaximumTermMonths: p.MaximumTermMonths))
            .OrderBy(m => m.EffectiveInterestRate)
            .ThenByDescending(m => m.MaximumAmount)
            .ToList();

        return matched;
    }

    private static decimal CalculateEffectiveRate(decimal baseRate, CreditTier tier)
    {
        return tier switch
        {
            CreditTier.A => baseRate,
            CreditTier.B => baseRate + 2m,
            CreditTier.C => baseRate + 4m,
            _ => baseRate
        };
    }
}
