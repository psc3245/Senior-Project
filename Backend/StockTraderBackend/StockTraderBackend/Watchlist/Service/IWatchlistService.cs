using StockTraderBackend.Watchlist;

public interface IWatchlistService
{
    Task<Watchlist> CreateWatchlist(
        Guid userId,
        string name,
        CancellationToken ct = default);

    Task DeleteWatchlist(
        Guid watchlistId,
        CancellationToken ct = default);

    Task<List<Watchlist>> GetUserWatchlists(
        Guid userId,
        CancellationToken ct = default);

    Task<Watchlist?> GetWatchlistById(
        Guid watchlistId,
        CancellationToken ct = default);

    Task<bool> UserOwnsWatchlist(
        Guid userId,
        Guid watchlistId,
        CancellationToken ct = default);
}