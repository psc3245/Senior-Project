using Microsoft.EntityFrameworkCore;
using StockTraderBackend.Assets.Symbols.DTOs;
using StockTraderBackend.Data;

namespace StockTraderBackend.Assets.Symbols
{
    public sealed class SymbolsRepository : ISymbolsRepository
    {
        private readonly AppDbContext _db;

        public SymbolsRepository(AppDbContext db) => _db = db;

        public async Task<SymbolRow?> GetByTickerAsync(string ticker, CancellationToken ct)
        {
            var normalized = ticker.Trim().ToUpperInvariant();

            return await _db.Symbols
                .AsNoTracking()
                .Where(s => s.Ticker == normalized)
                .Select(s => new SymbolRow(s.Id, s.Ticker))
                .FirstOrDefaultAsync(ct);
        }
        public async Task<SymbolRow?> GetByIdAsync(int symbolId, CancellationToken ct = default)
        {
            return await _db.Symbols
                .AsNoTracking()
                .Where(s => s.Id == symbolId)
                .Select(s => new SymbolRow(s.Id, s.Ticker))
                .FirstOrDefaultAsync(ct);
        }

        public async Task<List<SymbolRow>> GetActiveSymbolsAsync(CancellationToken ct = default)
        {
            return await _db.Symbols
                .AsNoTracking()
                .Where(s => s.IsActive)
                .Select(s => new SymbolRow(s.Id, s.Ticker))
                .ToListAsync(ct);
        }

        public async Task<List<SymbolDetailsRow>> GetAllSymbolsAsync(CancellationToken ct = default)
        {
            return await _db.Symbols
                .AsNoTracking()
                .OrderBy(s => s.Ticker)
                .Select(s => new SymbolDetailsRow(
                    s.Id,
                    s.Ticker,
                    s.Name,
                    s.Exchange,
                    s.AssetType,
                    s.IsActive,
                    s.CreatedAt
                ))
                .ToListAsync(ct);
        }

        public async Task<SymbolDetailsRow?> GetDetailsByTickerAsync(string ticker, CancellationToken ct = default)
        {
            var normalized = ticker.Trim().ToUpperInvariant();

            return await _db.Symbols
                .AsNoTracking()
                .Where(s => s.Ticker == normalized)
                .Select(s => new SymbolDetailsRow(
                    s.Id,
                    s.Ticker,
                    s.Name,
                    s.Exchange,
                    s.AssetType,
                    s.IsActive,
                    s.CreatedAt
                ))
                .FirstOrDefaultAsync(ct);
        }

        public async Task<SymbolDetailsRow?> GetDetailsByCompanyNameAsync(string companyName, CancellationToken ct = default)
        {
            var normalized = companyName.Trim().ToLower();

            if (string.IsNullOrWhiteSpace(normalized))
                return null;

            return await _db.Symbols
                .AsNoTracking()
                .Select(s => new
                {
                    Entity = s,

                    ExactMatch = s.Name.ToLower() == normalized,

                    StartsWithMatch = s.Name.ToLower().StartsWith(normalized),

                    ContainsMatch = s.Name.ToLower().Contains(normalized),

                    LengthDifference = Math.Abs(s.Name.Length - normalized.Length)
                })
                .Where(x =>
                    x.ExactMatch ||
                    x.StartsWithMatch ||
                    x.ContainsMatch)
                .OrderByDescending(x => x.ExactMatch)
                .ThenByDescending(x => x.StartsWithMatch)
                .ThenByDescending(x => x.ContainsMatch)
                .ThenBy(x => x.LengthDifference)
                .Select(x => new SymbolDetailsRow(
                    x.Entity.Id,
                    x.Entity.Ticker,
                    x.Entity.Name,
                    x.Entity.Exchange,
                    x.Entity.AssetType,
                    x.Entity.IsActive,
                    x.Entity.CreatedAt
                ))
                .FirstOrDefaultAsync(ct);
        }

        public async Task<List<SymbolBackfillRow>> GetSymbolsForBackfillScanAsync(CancellationToken ct = default)
        {
            return await _db.Symbols
                .AsNoTracking()
                .Where(s => s.IsActive)
                .OrderByDescending(s => s.NeedsBackfill)
                .ThenBy(s => s.LastBackfillAttemptUtc)
                .Select(s => new SymbolBackfillRow(
                    s.Id,
                    s.Ticker,
                    s.IsActive,
                    s.NeedsBackfill,
                    s.LastBackfillAttemptUtc,
                    s.LastBackfillSuccessUtc
                ))
                .ToListAsync(ct);
        }
        public async Task MarkBackfillAttemptAsync(int symbolId, CancellationToken ct = default)
        {
            var symbol = await _db.Symbols.FirstOrDefaultAsync(s => s.Id == symbolId, ct);
            if (symbol == null)
                return;

            symbol.LastBackfillAttemptUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }
        public async Task MarkBackfillSuccessAsync(int symbolId, CancellationToken ct = default)
        {
            var symbol = await _db.Symbols.FirstOrDefaultAsync(s => s.Id == symbolId, ct);
            if (symbol == null)
                return;

            symbol.NeedsBackfill = false;
            symbol.LastBackfillSuccessUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }
        public async Task MarkNeedsBackfillAsync(int symbolId, CancellationToken ct = default)
        {
            var symbol = await _db.Symbols.FirstOrDefaultAsync(s => s.Id == symbolId, ct);
            if (symbol == null)
                return;

            symbol.NeedsBackfill = true;
            await _db.SaveChangesAsync(ct);
        }
        public async Task<List<string>> GetAllCompanyNamesAsync(CancellationToken ct = default)
        {
            return await _db.Symbols
                .AsNoTracking()
                .OrderBy(s => s.Name)
                .Select(s => s.Name)
                .ToListAsync(ct);
        }
        public async Task<List<SymbolRow>> GetActiveSymbolsByTickersAsync(IEnumerable<string> tickers,CancellationToken ct = default)
        {
            var normalized = tickers
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => t.Trim().ToUpperInvariant())
                .Distinct()
                .ToList();

            if (normalized.Count == 0)
                return new List<SymbolRow>();

            return await _db.Symbols
                .AsNoTracking()
                .Where(s => s.IsActive && normalized.Contains(s.Ticker))
                .Select(s => new SymbolRow(s.Id, s.Ticker))
                .ToListAsync(ct);
        }

        public async Task MarkSplitScanUtcAsync(int symbolId, CancellationToken ct = default)
        {
            var symbol = await _db.Symbols.FirstOrDefaultAsync(s => s.Id == symbolId, ct);
            if (symbol == null)
                return;

            symbol.LastSplitScanUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }

        public async Task MarkSplitDetectedAsync(int symbolId, CancellationToken ct = default)
        {
            var symbol = await _db.Symbols.FirstOrDefaultAsync(s => s.Id == symbolId, ct);
            if (symbol == null)
                return;

            symbol.LastSplitDetectedUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }
        public async Task<List<SymbolResponseDto>> GetRandomSubsetOfSymbols(int count,CancellationToken ct = default)
        {
            count = Math.Clamp(count, 1, 100);

            return await _db.Symbols
                .AsNoTracking()
                .Where(s => s.IsActive)
                .OrderBy(s => Guid.NewGuid())
                .Take(count)
                .Select(s => new SymbolResponseDto
                {
                    Id = s.Id,
                    Ticker = s.Ticker,
                    Name = s.Name,
                    Exchange = s.Exchange,
                    AssetType = s.AssetType,
                    IsActive = s.IsActive,
                    CreatedAt = s.CreatedAt
                })
                .ToListAsync(ct);
        }
    }
}