using StockTraderBackend.MarketData.StockSplits.Models;
using Microsoft.EntityFrameworkCore;
using StockTraderBackend.Data;

namespace StockTraderBackend.MarketData.StockSplits.Repository
{
    public sealed class StockSplitsRepository : IStockSplitsRepository
    {
        private readonly AppDbContext _db;

        public StockSplitsRepository(AppDbContext db)
        {
            _db = db;
        }

        public async Task<bool> ExistsByMassiveSplitIdAsync(
            string massiveSplitId,
            CancellationToken ct = default)
        {
            return await _db.StockSplits
                .AsNoTracking()
                .AnyAsync(x => x.MassiveSplitId == massiveSplitId, ct);
        }
        public async Task<IReadOnlyList<StockSplit>> GetRecentSplitsAsync(
            DateOnly from,
            DateOnly to,
            int limit = 50,
            CancellationToken ct = default)
        {
            limit = Math.Clamp(limit, 1, 100);

            return await _db.StockSplits
                .AsNoTracking()
                .Include(s => s.Symbol)
                .Where(s => s.EffectiveDate >= from && s.EffectiveDate <= to)
                .OrderByDescending(s => s.EffectiveDate)
                .ThenBy(s => s.Symbol.Ticker)
                .Take(limit)
                .ToListAsync(ct);
        }

        public async Task<bool> ExistsBySymbolAndDateAsync(
            int symbolId,
            DateOnly effectiveDate,
            CancellationToken ct = default)
        {
            return await _db.StockSplits
                .AsNoTracking()
                .AnyAsync(x => x.SymbolId == symbolId && x.EffectiveDate == effectiveDate, ct);
        }

        public async Task AddAsync(
            StockSplit split,
            CancellationToken ct = default)
        {
            await _db.StockSplits.AddAsync(split, ct);
        }

        public async Task<StockSplit?> GetByIdAsync(
            int splitId,
            CancellationToken ct = default)
        {
            return await _db.StockSplits
                .Include(x => x.Symbol)
                .FirstOrDefaultAsync(x => x.Id == splitId, ct);
        }

        public async Task<IReadOnlyList<StockSplit>> GetBySymbolIdAsync(
            int symbolId,
            CancellationToken ct = default)
        {
            return await _db.StockSplits
                .AsNoTracking()
                .Include(x => x.Symbol)
                .Where(x => x.SymbolId == symbolId)
                .OrderByDescending(x => x.EffectiveDate)
                .ToListAsync(ct);
        }

        public void Remove(StockSplit split)
        {
            _db.StockSplits.Remove(split);
        }

        public async Task SaveChangesAsync(CancellationToken ct = default)
        {
            await _db.SaveChangesAsync(ct);
        }
    }
}
