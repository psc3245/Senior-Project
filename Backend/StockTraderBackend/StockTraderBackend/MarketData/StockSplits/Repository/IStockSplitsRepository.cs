using StockTraderBackend.MarketData.StockSplits.Models;

namespace StockTraderBackend.MarketData.StockSplits.Repository
{
    public interface IStockSplitsRepository
    {
        Task<bool> ExistsByMassiveSplitIdAsync(
            string massiveSplitId,
            CancellationToken ct = default);

        Task<bool> ExistsBySymbolAndDateAsync(
            int symbolId,
            DateOnly effectiveDate,
            CancellationToken ct = default);

        Task AddAsync(
            StockSplit split,
            CancellationToken ct = default);

        Task<StockSplit?> GetByIdAsync(
            int splitId,
            CancellationToken ct = default);

        Task<IReadOnlyList<StockSplit>> GetBySymbolIdAsync(
            int symbolId,
            CancellationToken ct = default);

        void Remove(StockSplit split);

        Task SaveChangesAsync(CancellationToken ct = default);
        Task<IReadOnlyList<StockSplit>> GetRecentSplitsAsync(DateOnly from, DateOnly to, int limit = 50, CancellationToken ct = default);
    }
}
