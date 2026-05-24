using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Domain.Entities;
using LoanSuperMarket.Infrastructure.Persistence;
using LoanSuperMarket.Shared.Common;
using LoanSuperMarket.Shared.Grids;
using LoanSuperMarket.Shared.Lenders;
using Microsoft.EntityFrameworkCore;

namespace LoanSuperMarket.Infrastructure.Repositories;

public sealed class LenderRepository : ILenderRepository
{
    private readonly ApplicationDbContext _context;

    public LenderRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Lender lender, CancellationToken cancellationToken)
    {
        await _context.Lenders.AddAsync(lender, cancellationToken);
    }

    public async Task<Lender?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.Lenders.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<Lender?> GetByUserIdAsync(string userId, CancellationToken cancellationToken)
    {
        return await _context.Lenders.FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
    }

    public async Task<IReadOnlyList<Lender>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _context.Lenders
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken)
    {
        return await _context.Lenders
            .AnyAsync(x => x.Email == email.Trim().ToLowerInvariant(), cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<PagedResult<LenderDto>> GetPagedAsync(
    GridQueryRequest request,
    CancellationToken cancellationToken)
    {
        request.PageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
        request.PageSize = request.PageSize is < 1 or > 100 ? 10 : request.PageSize;

        var query = _context.Lenders.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.SearchText))
        {
            var searchText = request.SearchText.Trim();

            query = query.Where(lender =>
                lender.CompanyName.Contains(searchText)
                || lender.ContactName.Contains(searchText)
                || lender.Email.Contains(searchText)
                || lender.PhoneNumber.Contains(searchText));
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            query = query.Where(lender =>
                lender.Status.ToString() == request.Status);
        }

        query = (request.SortColumn, request.SortDirection) switch
        {
            ("CompanyName", SortDirection.Asc) =>
                query.OrderBy(x => x.CompanyName),

            ("CompanyName", SortDirection.Desc) =>
                query.OrderByDescending(x => x.CompanyName),

            ("Email", SortDirection.Asc) =>
                query.OrderBy(x => x.Email),

            ("Email", SortDirection.Desc) =>
                query.OrderByDescending(x => x.Email),

            ("AvailableFunds", SortDirection.Asc) =>
                query.OrderBy(x => x.AvailableFunds),

            ("AvailableFunds", SortDirection.Desc) =>
                query.OrderByDescending(x => x.AvailableFunds),

            ("Status", SortDirection.Asc) =>
                query.OrderBy(x => x.Status),

            ("Status", SortDirection.Desc) =>
                query.OrderByDescending(x => x.Status),

            ("CreatedAtUtc", SortDirection.Asc) =>
                query.OrderBy(x => x.CreatedAtUtc),

            _ =>
                query.OrderByDescending(x => x.CreatedAtUtc)
        };

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(lender => new LenderDto
            {
                Id = lender.Id,
                CompanyName = lender.CompanyName,
                ContactName = lender.ContactName,
                Email = lender.Email,
                PhoneNumber = lender.PhoneNumber,
                AvailableFunds = lender.AvailableFunds,
                Status = lender.Status.ToString(),
                CreatedAtUtc = lender.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<LenderDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }
}