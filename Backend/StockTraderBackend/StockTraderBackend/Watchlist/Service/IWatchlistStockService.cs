using StockTraderBackend.Watchlist.DTOs;

namespace StockTraderBackend.Watchlist.Service
{
    public interface IWatchlistStockService
    {
        Task<WatchlistStock> AddStockToWatchlist(Guid watchlistId, AddStockRequest request);
        Task<WatchlistStock?> GetWatchlistStock(Guid watchlistId, int symbolId);
        Task<List<WatchlistStock>> GetWatchlistStocks(Guid watchlistId);
        Task RemoveStockFromWatchlist(Guid watchlistId, int symbolId);
    }
}