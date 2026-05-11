using StockTraderBackend.MarketData.StockSplits.Models;

namespace StockTraderBackend.MarketData.StockSplits.Service
{
    public interface IStockSplitDetectionService
    {
        Task<int> ScanRecentSplitsAsync(int lookbackDays = 14,CancellationToken ct = default);
        Task<IReadOnlyList<StockSplit>> GetSplitsForTickerAsync(string ticker,CancellationToken ct = default);
        Task<bool> DeleteSplitAsync(int splitId,CancellationToken ct = default);
        Task<IReadOnlyList<StockSplit>> GetRecentStoredSplitsAsync(int lookbackDays = 30, int limit = 50, CancellationToken ct = default);
    }
}
