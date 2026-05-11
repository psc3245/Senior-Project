using Microsoft.EntityFrameworkCore;
using StockTraderBackend.Data;
using StockTraderBackend.Assets.PriceBar.Models;
namespace StockTraderBackend.Assets.PriceBar
{
    public sealed class PriceBarsRepository : IPriceBarsRepository
    {
        private readonly AppDbContext _db;
        public PriceBarsRepository(AppDbContext db) => _db = db;

        public async Task<List<PriceBarRow>> GetDailyBarsAsync(
            int symbolId,
            DateOnly? from,
            DateOnly? to,
            int limit,
            CancellationToken ct)
        {
            IQueryable<Models.PriceBar> q = _db.PriceBars
                .AsNoTracking()
                .Where(pb => pb.SymbolId == symbolId && pb.Timeframe == "1d");

            if (from.HasValue)
            {
                var fromUtc = from.Value.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
                q = q.Where(pb => pb.Ts >= fromUtc);
            }

            if (to.HasValue)
            {
                var toExclusiveUtc = to.Value.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
                q = q.Where(pb => pb.Ts < toExclusiveUtc);
            }

            return await q
                .OrderByDescending(pb => pb.Ts)
                .Take(limit)
                .Select(pb => new PriceBarRow(
                    pb.Ts, pb.Open, pb.High, pb.Low, pb.Close, pb.Volume
                ))
                .ToListAsync(ct);
        }
        public async Task<decimal?> GetLatestCloseByTickerAsync(
            string ticker,
            string timeframe,
            CancellationToken ct = default)
        {
            return await _db.PriceBars
                .Where(pb =>
                    pb.Symbol.Ticker == ticker &&
                    pb.Timeframe == timeframe)
                .OrderByDescending(pb => pb.Ts)
                .Select(pb => (decimal?)pb.Close)
                .FirstOrDefaultAsync(ct);
        }

        public async Task<Dictionary<string, decimal>> GetLatestClosesByTickersAsync(
             IEnumerable<string> tickers,
             string timeframe,
             CancellationToken ct = default)
        {
            var normalized = tickers
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => t.Trim().ToUpperInvariant())
                .Distinct()
                .ToList();

            if (normalized.Count == 0)
            {
                return new Dictionary<string, decimal>();
            }

            var rows = await _db.PriceBars
                .AsNoTracking()
                .Where(pb =>
                    pb.Timeframe == timeframe &&
                    normalized.Contains(pb.Symbol.Ticker))
                .Select(pb => new
                {
                    Ticker = pb.Symbol.Ticker,
                    pb.Ts,
                    pb.Close
                })
                .ToListAsync(ct);

            return rows
                .GroupBy(r => r.Ticker)
                .Select(g => g
                    .OrderByDescending(r => r.Ts)
                    .First())
                .ToDictionary(
                    x => x.Ticker,
                    x => x.Close);
        }
        public async Task UpsertBarsAsync(IEnumerable<Models.PriceBar> bars, CancellationToken ct = default)
        {
            var incoming = bars.ToList();
            if (incoming.Count == 0)
                return;

            var symbolIds = incoming.Select(b => b.SymbolId).Distinct().ToList();
            var timeframes = incoming.Select(b => b.Timeframe).Distinct().ToList();

            var minTs = incoming.Min(b => b.Ts);
            var maxTs = incoming.Max(b => b.Ts);

            var existing = await _db.PriceBars
                .Where(pb =>
                    symbolIds.Contains(pb.SymbolId) &&
                    timeframes.Contains(pb.Timeframe) &&
                    pb.Ts >= minTs &&
                    pb.Ts <= maxTs)
                .ToListAsync(ct);

            var existingMap = existing.ToDictionary(
                pb => (pb.SymbolId, pb.Timeframe, pb.Ts));

            foreach (var bar in incoming)
            {
                var key = (bar.SymbolId, bar.Timeframe, bar.Ts);

                if (existingMap.TryGetValue(key, out var current))
                {
                    current.Open = bar.Open;
                    current.High = bar.High;
                    current.Low = bar.Low;
                    current.Close = bar.Close;
                    current.Volume = bar.Volume;
                }
                else
                {
                    _db.PriceBars.Add(bar);
                }
            }

            await _db.SaveChangesAsync(ct);
        }
        public async Task<PriceBarCoverageRow?> GetCoverageAsync(
            int symbolId,
            string timeframe,
            CancellationToken ct = default)
        {
            var query = _db.PriceBars
                .AsNoTracking()
                .Where(pb => pb.SymbolId == symbolId && pb.Timeframe == timeframe);

            var hasAny = await query.AnyAsync(ct);
            if (!hasAny)
                return null;

            var earliest = await query.MinAsync(pb => pb.Ts, ct);
            var latest = await query.MaxAsync(pb => pb.Ts, ct);

            return new PriceBarCoverageRow(earliest, latest);
        }
        public async Task<List<DateTime>> GetBarTimestampsAscAsync(
            int symbolId,
            string timeframe,
            CancellationToken ct = default)
        {
            return await _db.PriceBars
                .AsNoTracking()
                .Where(pb => pb.SymbolId == symbolId && pb.Timeframe == timeframe)
                .OrderBy(pb => pb.Ts)
                .Select(pb => pb.Ts)
                .ToListAsync(ct);
        }

        public async Task<int> DeleteBarsOlderThanAsync(
            int symbolId,
            string timeframe,
            DateTime cutoffUtc,
            CancellationToken ct = default)
        {
            return await _db.PriceBars
                .Where(x =>
                    x.SymbolId == symbolId &&
                    x.Timeframe == timeframe &&
                    x.Ts < cutoffUtc)
                .ExecuteDeleteAsync(ct);
        }
    }
}
