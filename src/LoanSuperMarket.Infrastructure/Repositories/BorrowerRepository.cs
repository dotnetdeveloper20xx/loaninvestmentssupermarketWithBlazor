using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Domain.Entities;
using LoanSuperMarket.Infrastructure.Persistence;
using LoanSuperMarket.Shared.Borrowers;
using LoanSuperMarket.Shared.Common;
using LoanSuperMarket.Shared.Grids;
using Microsoft.EntityFrameworkCore;

namespace LoanSuperMarket.Infrastructure.Repositories;

public sealed class BorrowerRepository : IBorrowerRepository
{
    private readonly ApplicationDbContext _context;

    public BorrowerRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Borrower borrower, CancellationToken cancellationToken)
    {
        await _context.Borrowers.AddAsync(borrower, cancellationToken);
    }

    public async Task<Borrower?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return await _context.Borrowers
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<Borrower?> GetByUserIdAsync(string userId, CancellationToken cancellationToken)
    {
        return await _context.Borrowers
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
    }

    public async Task<IReadOnlyList<Borrower>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _context.Borrowers
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken)
    {
        return await _context.Borrowers
            .AnyAsync(x => x.Email == email.Trim().ToLowerInvariant(), cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<PagedResult<BorrowerDto>> GetPagedAsync(
    GridQueryRequest request,
    CancellationToken cancellationToken)
    {
        request.PageNumber = request.PageNumber < 1 ? 1 : request.PageNumber;
        request.PageSize = request.PageSize is < 1 or > 100 ? 10 : request.PageSize;

        var query = _context.Borrowers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.SearchText))
        {
            var searchText = request.SearchText.Trim();

            query = query.Where(borrower =>
                borrower.FirstName.Contains(searchText)
                || borrower.LastName.Contains(searchText)
                || borrower.Email.Contains(searchText)
                || borrower.PhoneNumber.Contains(searchText));
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            query = query.Where(borrower =>
                borrower.Status.ToString() == request.Status);
        }

        query = (request.SortColumn, request.SortDirection) switch
        {
            ("FullName", SortDirection.Asc) =>
                query.OrderBy(x => x.FirstName).ThenBy(x => x.LastName),

            ("FullName", SortDirection.Desc) =>
                query.OrderByDescending(x => x.FirstName).ThenByDescending(x => x.LastName),

            ("Email", SortDirection.Asc) =>
                query.OrderBy(x => x.Email),

            ("Email", SortDirection.Desc) =>
                query.OrderByDescending(x => x.Email),

            ("DateOfBirth", SortDirection.Asc) =>
                query.OrderBy(x => x.DateOfBirth),

            ("DateOfBirth", SortDirection.Desc) =>
                query.OrderByDescending(x => x.DateOfBirth),

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
            .Select(borrower => new BorrowerDto
            {
                Id = borrower.Id,
                FirstName = borrower.FirstName,
                LastName = borrower.LastName,
                FullName = borrower.FirstName + " " + borrower.LastName,
                Email = borrower.Email,
                PhoneNumber = borrower.PhoneNumber,
                DateOfBirth = borrower.DateOfBirth,
                Status = borrower.Status.ToString(),
                CreatedAtUtc = borrower.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<BorrowerDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }
}