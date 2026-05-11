using StockTraderBackend.Trades.DTOs;

namespace StockTraderBackend.Trades.Services
{
    public interface ITradeEstimateService
    {
        Task<TradeEstimateDto> EstimateTradeAsync(
            Guid userId,
            TradeType action,
            string ticker,
            decimal quantity,
            CancellationToken ct = default);
    }
}
