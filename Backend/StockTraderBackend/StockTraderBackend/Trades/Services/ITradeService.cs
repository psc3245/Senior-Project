using StockTraderBackend.Portfolios.DTOs;
using StockTraderBackend.Trades.DTOs;

namespace StockTraderBackend.Trades.Services
{
    public interface ITradeService
    {
        Task<PortfolioDTO> handleBuy(Guid portfolioId, TradeRequest tradeRequest, CancellationToken ct = default);
        Task<PortfolioDTO> handleSell(Guid portfolioId, TradeRequest tradeRequest, CancellationToken ct = default);
        Task<PortfolioDTO> handleTrade(Guid portfolioId, TradeRequest tradeRequest);
        void saveTrade(Guid portfolioId, TradeRequest tradeRequest, decimal sharePrice);
    }
}