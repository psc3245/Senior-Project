using StockTraderBackend.Middleware;
using StockTraderBackend.Portfolios.DTOs;
using StockTraderBackend.Portfolios.Services;
using StockTraderBackend.StockAPI;

namespace StockTraderBackend.Portfolios.Performance;

public sealed class PortfolioPerformanceService : IPortfolioPerformanceService
{
    private readonly IPortfolioService _portfolioService;
    private readonly IStockAPIService _stockApiService;

    public PortfolioPerformanceService(
        IPortfolioService portfolioService,
        IStockAPIService stockApiService)
    {
        _portfolioService = portfolioService;
        _stockApiService = stockApiService;
    }

    public async Task<PortfolioPerformanceSummaryDto> GetPortfolioPerformanceSummaryAsync(
        Guid userId,
        Guid portfolioId,
        CancellationToken ct = default)
    {
        var ownsPortfolio = await _portfolioService.UserOwnsPortfolio(userId, portfolioId, ct);

        if (!ownsPortfolio)
        {
            throw new ApiException(403, "User does not own this portfolio.");
        }

        var portfolio = await _portfolioService.getPortfolioEntityById(portfolioId, ct);

        if (portfolio == null)
        {
            throw new ApiException(404, $"Portfolio '{portfolioId}' not found.");
        }

        var positions = new List<PortfolioPositionPerformanceDto>();

        foreach (var holding in portfolio.holdings)
        {
            var currentPrice = await _stockApiService.getPriceByTickerAsync(holding.ticker)
                ?? holding.avgBuyPrice;

            var marketValue = holding.quantity * currentPrice;
            var costBasis = holding.quantity * holding.avgBuyPrice;
            var gainLoss = marketValue - costBasis;

            var gainLossPercent = costBasis == 0
                ? 0
                : gainLoss / costBasis * 100;

            positions.Add(new PortfolioPositionPerformanceDto
            {
                Ticker = holding.ticker,
                Quantity = holding.quantity,
                AverageBuyPrice = Math.Round(holding.avgBuyPrice, 2),
                CurrentPrice = Math.Round(currentPrice, 2),
                MarketValue = Math.Round(marketValue, 2),
                CostBasis = Math.Round(costBasis, 2),
                GainLoss = Math.Round(gainLoss, 2),
                GainLossPercent = Math.Round(gainLossPercent, 2)
            });
        }

        var holdingsValue = positions.Sum(p => p.MarketValue);
        var totalCostBasis = positions.Sum(p => p.CostBasis);
        var totalGainLoss = holdingsValue - totalCostBasis;

        var totalGainLossPercent = totalCostBasis == 0
            ? 0
            : totalGainLoss / totalCostBasis * 100;

        return new PortfolioPerformanceSummaryDto
        {
            PortfolioId = portfolio.portfolioId,
            Name = portfolio.name,

            CashBalance = Math.Round(portfolio.cashBalance, 2),
            HoldingsValue = Math.Round(holdingsValue, 2),
            TotalPortfolioValue = Math.Round(portfolio.cashBalance + holdingsValue, 2),

            TotalCostBasis = Math.Round(totalCostBasis, 2),
            TotalGainLoss = Math.Round(totalGainLoss, 2),
            TotalGainLossPercent = Math.Round(totalGainLossPercent, 2),

            TopGainer = positions.OrderByDescending(p => p.GainLossPercent).FirstOrDefault(),
            TopLoser = positions.OrderBy(p => p.GainLossPercent).FirstOrDefault(),

            Positions = positions
        };
    }
}