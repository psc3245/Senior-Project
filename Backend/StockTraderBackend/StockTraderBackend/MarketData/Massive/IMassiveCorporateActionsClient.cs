using StockTraderBackend.MarketData.Massive.DTOs;

namespace StockTraderBackend.MarketData.Massive
{
    public interface IMassiveCorporateActionsClient
    {
        Task<IReadOnlyList<MassiveSplitDto>> GetSplitsAsync(
            DateOnly fromDate,
            DateOnly toDate,
            CancellationToken ct = default);
    }
}
