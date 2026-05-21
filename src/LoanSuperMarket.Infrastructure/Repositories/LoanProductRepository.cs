using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Domain.Entities;
using LoanSuperMarket.Infrastructure.Persistence;
using LoanSuperMarket.Shared.Common;
using LoanSuperMarket.Shared.Grids;
using LoanSuperMarket.Shared.LoanProducts;
using Microsoft.EntityFrameworkCore;

namespace LoanSuperMarket.Infrastructure.Repositories;

public sealed class LoanProductRepository : ILoanProductRepository
{
    private readonly ApplicationDbContext _context;

    public LoanProductRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(LoanProduct loanProduct, CancellationToken cancellationToken)
    {
        await _context.LoanProducts.AddAsync(loanProduct, cancellationToken);
    }

    public async Task<LoanProduct?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.LoanProducts
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<LoanProduct>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _context.LoanProducts
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<PagedResult<LoanProductDto>> GetPagedAsync(
    GridQueryRequest request,
    CancellationToken cancellationToken)
    {
        request.PageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
        request.PageSize = request.PageSize is < 1 or > 100 ? 10 : request.PageSize;

        var query = _context.LoanProducts.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.SearchText))
        {
            var searchText = request.SearchText.Trim();

            query = query.Where(product =>
                product.Title.Contains(searchText)
                || product.Description.Contains(searchText));
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            query = query.Where(product =>
                product.Status.ToString() == request.Status);
        }

        query = (request.SortColumn, request.SortDirection) switch
        {
            ("Title", SortDirection.Asc) => query.OrderBy(x => x.Title),
            ("Title", SortDirection.Desc) => query.OrderByDescending(x => x.Title),

            ("MinimumAmount", SortDirection.Asc) => query.OrderBy(x => x.MinimumAmount.Amount),
            ("MinimumAmount", SortDirection.Desc) => query.OrderByDescending(x => x.MinimumAmount.Amount),

            ("MaximumAmount", SortDirection.Asc) => query.OrderBy(x => x.MaximumAmount.Amount),
            ("MaximumAmount", SortDirection.Desc) => query.OrderByDescending(x => x.MaximumAmount.Amount),

            ("InterestRate", SortDirection.Asc) => query.OrderBy(x => x.InterestRate),
            ("InterestRate", SortDirection.Desc) => query.OrderByDescending(x => x.InterestRate),

            ("MinimumTermMonths", SortDirection.Asc) => query.OrderBy(x => x.MinimumTermMonths),
            ("MinimumTermMonths", SortDirection.Desc) => query.OrderByDescending(x => x.MinimumTermMonths),

            ("MaximumTermMonths", SortDirection.Asc) => query.OrderBy(x => x.MaximumTermMonths),
            ("MaximumTermMonths", SortDirection.Desc) => query.OrderByDescending(x => x.MaximumTermMonths),

            ("Status", SortDirection.Asc) => query.OrderBy(x => x.Status),
            ("Status", SortDirection.Desc) => query.OrderByDescending(x => x.Status),

            ("CreatedAtUtc", SortDirection.Asc) => query.OrderBy(x => x.CreatedAtUtc),
            _ => query.OrderByDescending(x => x.CreatedAtUtc)
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(product => new LoanProductDto
            {
                Id = product.Id,
                LenderId = product.LenderId,
                Title = product.Title,
                Description = product.Description,
                MinimumAmount = product.MinimumAmount.Amount,
                MaximumAmount = product.MaximumAmount.Amount,
                Currency = product.MinimumAmount.Currency,
                InterestRate = product.InterestRate.Percentage,
                MinimumTermMonths = product.MinimumTermMonths,
                MaximumTermMonths = product.MaximumTermMonths,
                Status = product.Status.ToString(),
                CreatedAtUtc = product.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<LoanProductDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }
}