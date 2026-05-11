using StockTraderBackend.Holdings;
using StockTraderBackend.Portfolios.DTOs;
using StockTraderBackend.Trades.DTOs;

namespace StockTraderBackend.Portfolios
{
    public interface IPortfolioService
    {
        Task addCash(Guid portfolioId, decimal amount, CancellationToken ct = default);
        Task createPortfolio(Portfolio portfolio, CancellationToken ct = default);
        Task deductCash(Guid portfolioId, decimal amount, CancellationToken ct = default);
        Task deletePortfolio(Guid id, CancellationToken ct = default);
        Task<ICollection<PortfolioDTO>> getAllPortfolios(CancellationToken ct = default);
        Task<PortfolioDTO> getPortfolioById(Guid id, CancellationToken ct = default);
        Task<PortfolioDTO> getPortfolioByUserId(Guid userId, CancellationToken ct = default);
        Task<Portfolio?> getPortfolioEntityById(Guid id, CancellationToken ct = default);
        Task<Portfolio?> getPortfolioEntityByUserId(Guid userId, CancellationToken ct = default);
        Task tryHandleSell(Guid portfolioId, TradeRequest tradeRequest, decimal price, CancellationToken ct = default);
        Task updatePortfolio(Portfolio portfolio, CancellationToken ct = default);
        Task upsertHolding(Guid portfolioId, Holding holding, CancellationToken ct = default);
        Task<bool> UserOwnsPortfolio(Guid userId, Guid portfolioId, CancellationToken ct = default);
    }
}