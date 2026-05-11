using StockTraderBackend.Data;
using Microsoft.EntityFrameworkCore;

namespace StockTraderBackend.Trades.Repositories;

public class TradeRepository
{
    private readonly AppDbContext _db;

    public TradeRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task addTrade(Trade trade, CancellationToken ct = default)
    {
        _db.Trades.Add(trade);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<Trade?> getTradeById(Guid tradeId, CancellationToken ct = default)
    {
        return await _db.Trades
            .FirstOrDefaultAsync(t => t.tradeId == tradeId, ct);
    }

    public async Task<List<Trade>> getTradesByPortfolioId(Guid portfolioId, CancellationToken ct = default)
    {
        return await _db.Trades
            .Where(t => t.portfolioId == portfolioId)
            .OrderByDescending(t => t.executedAt)
            .ToListAsync(ct);
    }

    public async Task<List<Trade>> getTradesByTicker(string ticker, CancellationToken ct = default)
    {
        return await _db.Trades
            .Where(t => t.ticker == ticker)
            .OrderByDescending(t => t.executedAt)
            .ToListAsync(ct);
    }

    public async Task<List<Trade>> getTrades(CancellationToken ct = default)
    {
        return await _db.Trades
            .OrderByDescending(t => t.executedAt)
            .ToListAsync(ct);
    }

    public async Task deleteTradeById(Guid tradeId, CancellationToken ct = default)
    {
        var old = await _db.Trades
            .FirstOrDefaultAsync(t => t.tradeId == tradeId, ct);

        if (old == null)
            throw new KeyNotFoundException($"Trade with id {tradeId} not found");

        _db.Trades.Remove(old);
        await _db.SaveChangesAsync(ct);
    }

    public async Task deleteTrade(Trade trade, CancellationToken ct = default)
    {
        _db.Trades.Remove(trade);
        await _db.SaveChangesAsync(ct);
    }
}