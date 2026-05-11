using StockTraderBackend.Data;
using Microsoft.EntityFrameworkCore;
using StockTraderBackend.Watchlist.DTOs;
using StockTraderBackend.Assets.Symbols.Model;

namespace StockTraderBackend.Watchlist.Service;

public class WatchlistStockService : IWatchlistStockService
{
    private readonly AppDbContext _db;

    public WatchlistStockService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<WatchlistStock?> GetWatchlistStock(Guid watchlistId, int symbolId)
    {
        return await _db.WatchlistStocks.FindAsync(watchlistId, symbolId);
    }

    public async Task<List<WatchlistStock>> GetWatchlistStocks(Guid watchlistId)
    {
        return await _db.WatchlistStocks
            .Where(ws => ws.WatchlistId == watchlistId)
            .OrderBy(ws => ws.Position)
            .Include(ws => ws.Symbol)
            .ToListAsync();
    }

    public async Task<WatchlistStock> AddStockToWatchlist(Guid watchlistId, AddStockRequest request)
    {
        if (request == null)
            throw new Exception("Request is required");

        var hasSymbolId = request.SymbolId.HasValue;
        var hasTicker = !string.IsNullOrWhiteSpace(request.Ticker);

        if (hasSymbolId == hasTicker)
            throw new Exception("Provide exactly one of SymbolId or Ticker");

        Symbol? symbol;

        if (hasSymbolId)
        {
            symbol = await _db.Symbols.FindAsync(request.SymbolId!.Value);
            if (symbol == null)
                throw new Exception("Symbol not found");
        }
        else
        {
            var normalizedTicker = request.Ticker!.Trim().ToUpper();

            symbol = await _db.Symbols
                .FirstOrDefaultAsync(s => s.Ticker.ToUpper() == normalizedTicker);

            if (symbol == null)
                throw new Exception("Symbol not found");
        }

        var existing = await GetWatchlistStock(watchlistId, symbol.Id);
        if (existing != null)
            throw new Exception("Stock already in watchlist");

        var maxPosition = await _db.WatchlistStocks
            .Where(ws => ws.WatchlistId == watchlistId)
            .MaxAsync(ws => (int?)ws.Position) ?? -1;

        var watchlistStock = new WatchlistStock
        {
            WatchlistId = watchlistId,
            SymbolId = symbol.Id,
            Position = maxPosition + 1,
            AddedAt = DateTime.UtcNow,
            Symbol = symbol
        };

        _db.WatchlistStocks.Add(watchlistStock);
        await _db.SaveChangesAsync();

        return watchlistStock;
    }

    public async Task RemoveStockFromWatchlist(Guid watchlistId, int symbolId)
    {
        var watchlistStock = await GetWatchlistStock(watchlistId, symbolId);
        if (watchlistStock == null)
            throw new Exception("Stock not found in watchlist");

        _db.WatchlistStocks.Remove(watchlistStock);
        await _db.SaveChangesAsync();
    }
}