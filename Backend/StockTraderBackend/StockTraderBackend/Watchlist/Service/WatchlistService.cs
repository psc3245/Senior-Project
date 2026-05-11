using StockTraderBackend.Watchlist;
using StockTraderBackend.Watchlist.Repository;

public class WatchlistService : IWatchlistService
{
    private readonly IWatchlistRepository _watchlistRepository;

    public WatchlistService(IWatchlistRepository watchlistRepository)
    {
        _watchlistRepository = watchlistRepository;
    }

    public async Task<List<Watchlist>> GetUserWatchlists(
        Guid userId,
        CancellationToken ct = default)
    {
        return await _watchlistRepository.GetUserWatchlistsAsync(userId, ct);
    }

    public async Task<Watchlist?> GetWatchlistById(
        Guid watchlistId,
        CancellationToken ct = default)
    {
        return await _watchlistRepository.GetWatchlistByIdAsync(watchlistId, ct);
    }

    public async Task<bool> UserOwnsWatchlist(
        Guid userId,
        Guid watchlistId,
        CancellationToken ct = default)
    {
        return await _watchlistRepository.UserOwnsWatchlistAsync(
            userId,
            watchlistId,
            ct);
    }

    public async Task<Watchlist> CreateWatchlist(
        Guid userId,
        string name,
        CancellationToken ct = default)
    {
        var watchlist = new Watchlist
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = string.IsNullOrWhiteSpace(name) ? "Default" : name,
            CreatedAt = DateTime.UtcNow,
            Stocks = new List<WatchlistStock>()
        };

        await _watchlistRepository.AddAsync(watchlist, ct);
        await _watchlistRepository.SaveChangesAsync(ct);

        return watchlist;
    }

    public async Task DeleteWatchlist(
        Guid watchlistId,
        CancellationToken ct = default)
    {
        var watchlist = await _watchlistRepository.GetWatchlistByIdAsync(
            watchlistId,
            ct);

        if (watchlist == null)
        {
            throw new Exception("Watchlist not found");
        }

        await _watchlistRepository.DeleteAsync(watchlist, ct);
        await _watchlistRepository.SaveChangesAsync(ct);
    }
}