using StockTraderBackend.MarketData.Massive.DTOs;

namespace StockTraderBackend.MarketData.Massive
{
    public interface IMassiveMarketDataClient
    {
        Task<MassiveGroupedDailyResponse?> GetGroupedDailyAsync(
            DateOnly date,
            CancellationToken ct = default);

        Task<MassiveTickerRangeResponse?> GetDailyBarsAsync(
            string ticker,
            DateOnly from,
            DateOnly to,
            CancellationToken ct = default);
    }
}
