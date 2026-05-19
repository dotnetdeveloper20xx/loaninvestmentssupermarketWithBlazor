using LoanSuperMarket.Application.Common.Interfaces;
using LoanSuperMarket.Domain.Entities;
using LoanSuperMarket.Infrastructure.Persistence;
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
}