using StockTraderBackend.Watchlist;

namespace StockTraderBackend.Watchlist.Repository;

public interface IWatchlistRepository
{
    Task<List<Watchlist>> GetUserWatchlistsAsync(
        Guid userId,
        CancellationToken ct = default);

    Task<Watchlist?> GetWatchlistByIdAsync(
        Guid watchlistId,
        CancellationToken ct = default);

    Task<bool> UserOwnsWatchlistAsync(
        Guid userId,
        Guid watchlistId,
        CancellationToken ct = default);

    Task AddAsync(
        Watchlist watchlist,
        CancellationToken ct = default);

    Task DeleteAsync(
        Watchlist watchlist,
        CancellationToken ct = default);

    Task SaveChangesAsync(
        CancellationToken ct = default);
}