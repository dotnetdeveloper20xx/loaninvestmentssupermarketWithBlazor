using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Domain.Entities;
using LoanSuperMarket.Infrastructure.Persistence;
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
}