using StockTraderBackend.Portfolios.DTOs;

namespace StockTraderBackend.Portfolios.Services;

public interface IPortfolioPerformanceService
{
    Task<PortfolioPerformanceSummaryDto> GetPortfolioPerformanceSummaryAsync(
        Guid userId,
        Guid portfolioId,
        CancellationToken ct = default);
}