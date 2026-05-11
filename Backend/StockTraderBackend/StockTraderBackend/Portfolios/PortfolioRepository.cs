using StockTraderBackend.Data;
using Microsoft.EntityFrameworkCore;
namespace StockTraderBackend.Portfolios;

public class PortfolioRepository
{
    private readonly AppDbContext _db;

    public PortfolioRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task addPortfolio(Portfolio portfolio, CancellationToken ct = default)
    {
        _db.Portfolios.Add(portfolio);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<Portfolio?> getPortfolioById(Guid portfolioId, CancellationToken ct = default)
    {
        return await _db.Portfolios
            .Include(p => p.holdings)
            .Include(p => p.trades)
            .FirstOrDefaultAsync(p => p.portfolioId == portfolioId, ct);
    }

    public async Task<Portfolio?> getPortfolioByUserId(Guid userId, CancellationToken ct = default)
    {
        return await _db.Portfolios
            .Include(p => p.holdings)
            .Include(p => p.trades)
            .FirstOrDefaultAsync(p => p.userId == userId, ct);
    }

    public async Task<List<Portfolio>> getPortfolios(CancellationToken ct = default)
    {
        return await _db.Portfolios
            .Include(p => p.holdings)
            .Include(p => p.trades)
            .ToListAsync(ct);
    }
    public async Task<Portfolio?> getPortfolioEntityByUserId(
        Guid userId,
        CancellationToken ct = default)
    {
        return await _db.Portfolios
            .Include(p => p.holdings)
            .Include(p => p.trades)
            .FirstOrDefaultAsync(p => p.userId == userId, ct);
    }

    // Option A (recommended): update by attaching + marking modified
    public async Task updatePortfolio(Portfolio portfolio, CancellationToken ct = default)
    {
        _db.Portfolios.Update(portfolio);
        await _db.SaveChangesAsync(ct);
    }

    public async Task deletePortfolioById(Guid portfolioId, CancellationToken ct = default)
    {
        var old = await _db.Portfolios
            .Include(p => p.holdings)
            .Include(p => p.trades)
            .FirstOrDefaultAsync(p => p.portfolioId == portfolioId, ct);

        if (old == null)
            throw new KeyNotFoundException($"Portfolio with id {portfolioId} not found");

        _db.Portfolios.Remove(old);
        await _db.SaveChangesAsync(ct);
    }

    public async Task deletePortfolio(Portfolio portfolio, CancellationToken ct = default)
    {
        _db.Portfolios.Remove(portfolio);
        await _db.SaveChangesAsync(ct);
    }
}