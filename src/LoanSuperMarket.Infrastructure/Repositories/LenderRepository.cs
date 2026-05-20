using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Domain.Entities;
using LoanSuperMarket.Infrastructure.Persistence;
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
}