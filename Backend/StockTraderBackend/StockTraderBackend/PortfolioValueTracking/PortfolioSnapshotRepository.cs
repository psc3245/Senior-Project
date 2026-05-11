using Microsoft.EntityFrameworkCore;
using StockTraderBackend.Data;

namespace StockTraderBackend.PortfolioValueTracking;

public sealed class PortfolioSnapshotRepository
{
    private readonly AppDbContext _db;
    
    public PortfolioSnapshotRepository(AppDbContext db) => _db = db;

    public async Task UpsertPortfolioSnapshot(PortfolioSnapshot portfolioSnapshot, CancellationToken ct)
    {
        var existing = await _db.PortfolioSnapshots
            .FirstOrDefaultAsync(ps => ps.PortfolioId == portfolioSnapshot.PortfolioId 
                                       && ps.Date == portfolioSnapshot.Date, ct);

        if (existing != null)
        {
            _db.PortfolioSnapshots.Remove(existing);
        }

        _db.PortfolioSnapshots.Add(portfolioSnapshot);
        await _db.SaveChangesAsync(ct);
    }
    
    public async Task<List<PortfolioSnapshot>> GetSnapshotsForPortfolioAsync(
        Guid portfolioId,
        DateOnly? from,
        DateOnly? to,
        CancellationToken ct = default)
    {
        IQueryable<PortfolioSnapshot> q = _db.PortfolioSnapshots
            .AsNoTracking()
            .Include(s => s.Holdings)
            .Where(s => s.PortfolioId == portfolioId);

        if (from.HasValue)
            q = q.Where(s => s.Date >= from.Value);

        if (to.HasValue)
            q = q.Where(s => s.Date <= to.Value);

        return await q
            .OrderBy(s => s.Date)
            .ToListAsync(ct);
    }
}