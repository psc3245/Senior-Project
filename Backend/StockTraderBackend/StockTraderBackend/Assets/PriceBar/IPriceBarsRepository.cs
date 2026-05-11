namespace StockTraderBackend.Assets.PriceBar
{
    using StockTraderBackend.Assets.PriceBar.Models;

    public interface IPriceBarsRepository
    {
        Task<List<PriceBarRow>> GetDailyBarsAsync(
            int symbolId,
            DateOnly? from,
            DateOnly? to,
            int limit,
            CancellationToken ct);

        Task<decimal?> GetLatestCloseByTickerAsync(
            string ticker,
            string timeframe,
            CancellationToken ct = default);

        Task<Dictionary<string, decimal>> GetLatestClosesByTickersAsync(
            IEnumerable<string> tickers,
            string timeframe,
            CancellationToken ct = default);

        Task UpsertBarsAsync(IEnumerable<PriceBar> bars, CancellationToken ct = default);

        Task<PriceBarCoverageRow?> GetCoverageAsync(
            int symbolId,
            string timeframe,
            CancellationToken ct = default);

        Task<List<DateTime>> GetBarTimestampsAscAsync(
            int symbolId,
            string timeframe,
            CancellationToken ct = default);
        Task<int> DeleteBarsOlderThanAsync(
            int symbolId,
            string timeframe,
            DateTime cutoffUtc,
            CancellationToken ct = default);

    }

    public sealed record PriceBarRow(
        DateTime TsUtc,
        decimal Open,
        decimal High,
        decimal Low,
        decimal Close,
        decimal Volume
    );
    public record PriceBarCoverageRow(DateTime EarliestTs, DateTime LatestTs);

}
