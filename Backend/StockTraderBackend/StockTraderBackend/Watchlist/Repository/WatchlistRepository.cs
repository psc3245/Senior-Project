using Microsoft.EntityFrameworkCore;
using StockTraderBackend.Data;

namespace StockTraderBackend.Watchlist.Repository;

public sealed class WatchlistRepository : IWatchlistRepository
{
    private readonly AppDbContext _db;

    public WatchlistRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<Watchlist>> GetUserWatchlistsAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        return await _db.Watchlists
            .Where(w => w.UserId == userId)
            .Include(w => w.Stocks)
            .ThenInclude(ws => ws.Symbol)
            .OrderBy(w => w.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<Watchlist?> GetWatchlistByIdAsync(
        Guid watchlistId,
        CancellationToken ct = default)
    {
        return await _db.Watchlists
            .Include(w => w.Stocks)
            .ThenInclude(ws => ws.Symbol)
            .FirstOrDefaultAsync(w => w.Id == watchlistId, ct);
    }

    public async Task<bool> UserOwnsWatchlistAsync(
        Guid userId,
        Guid watchlistId,
        CancellationToken ct = default)
    {
        return await _db.Watchlists
            .AsNoTracking()
            .AnyAsync(w =>
                w.Id == watchlistId &&
                w.UserId == userId,
                ct);
    }

    public async Task AddAsync(
        Watchlist watchlist,
        CancellationToken ct = default)
    {
        await _db.Watchlists.AddAsync(watchlist, ct);
    }

    public Task DeleteAsync(
        Watchlist watchlist,
        CancellationToken ct = default)
    {
        _db.Watchlists.Remove(watchlist);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(
        CancellationToken ct = default)
    {
        await _db.SaveChangesAsync(ct);
    }
}