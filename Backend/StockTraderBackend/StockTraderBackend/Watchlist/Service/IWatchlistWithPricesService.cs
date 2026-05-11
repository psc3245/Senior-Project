using StockTraderBackend.Watchlist.DTOs;

namespace StockTraderBackend.Watchlist.Service
{
    public interface IWatchlistWithPricesService
    {
        Task<WatchlistWithPricesDto> GetWatchlistWithPricesAsync(
            Guid userId,
            Guid watchlistId,
            CancellationToken ct = default);
    }
}
