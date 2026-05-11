using Moq;
using StockTraderBackend.Holdings;
using StockTraderBackend.Middleware;
using StockTraderBackend.Portfolios;
using StockTraderBackend.Portfolios.Performance;
using StockTraderBackend.StockAPI;

namespace StockTraderBackend.Tests.Portfolios;

public sealed class PortfolioPerformanceServiceTests
{
    private readonly Mock<IPortfolioService> _portfolioService = new();
    private readonly Mock<IStockAPIService> _stockApiService = new();

    private PortfolioPerformanceService CreateService()
    {
        return new PortfolioPerformanceService(
            _portfolioService.Object,
            _stockApiService.Object);
    }

    private static Portfolio CreatePortfolio(
        Guid userId,
        Guid portfolioId,
        string name,
        decimal cashBalance,
        List<Holding>? holdings = null)
    {
        var portfolio = (Portfolio)Activator.CreateInstance(
            typeof(Portfolio),
            nonPublic: true)!;

        portfolio.userId = userId;
        portfolio.portfolioId = portfolioId;
        portfolio.name = name;
        portfolio.cashBalance = cashBalance;
        portfolio.holdings = holdings ?? new List<Holding>();

        return portfolio;
    }

    [Fact]
    public async Task GetPortfolioPerformanceSummaryAsync_Throws403_WhenUserDoesNotOwnPortfolio()
    {
        var service = CreateService();

        var userId = Guid.NewGuid();
        var portfolioId = Guid.NewGuid();

        _portfolioService
            .Setup(x => x.UserOwnsPortfolio(
                userId,
                portfolioId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var ex = await Assert.ThrowsAsync<ApiException>(() =>
            service.GetPortfolioPerformanceSummaryAsync(userId, portfolioId));

        Assert.Equal(403, ex.StatusCode);

        _portfolioService.Verify(
            x => x.getPortfolioEntityById(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetPortfolioPerformanceSummaryAsync_Throws404_WhenPortfolioDoesNotExist()
    {
        var service = CreateService();

        var userId = Guid.NewGuid();
        var portfolioId = Guid.NewGuid();

        _portfolioService
            .Setup(x => x.UserOwnsPortfolio(
                userId,
                portfolioId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _portfolioService
            .Setup(x => x.getPortfolioEntityById(
                portfolioId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Portfolio?)null);

        var ex = await Assert.ThrowsAsync<ApiException>(() =>
            service.GetPortfolioPerformanceSummaryAsync(userId, portfolioId));

        Assert.Equal(404, ex.StatusCode);
    }

    [Fact]
    public async Task GetPortfolioPerformanceSummaryAsync_ReturnsZeros_WhenPortfolioHasNoHoldings()
    {
        var service = CreateService();

        var userId = Guid.NewGuid();
        var portfolioId = Guid.NewGuid();

        var portfolio = CreatePortfolio(
            userId,
            portfolioId,
            "Test Portfolio",
            500m);

        _portfolioService
            .Setup(x => x.UserOwnsPortfolio(
                userId,
                portfolioId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _portfolioService
            .Setup(x => x.getPortfolioEntityById(
                portfolioId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(portfolio);

        var result = await service.GetPortfolioPerformanceSummaryAsync(userId, portfolioId);

        Assert.Equal(portfolioId, result.PortfolioId);
        Assert.Equal("Test Portfolio", result.Name);
        Assert.Equal(500m, result.CashBalance);
        Assert.Equal(0m, result.HoldingsValue);
        Assert.Equal(500m, result.TotalPortfolioValue);
        Assert.Equal(0m, result.TotalCostBasis);
        Assert.Equal(0m, result.TotalGainLoss);
        Assert.Equal(0m, result.TotalGainLossPercent);
        Assert.Null(result.TopGainer);
        Assert.Null(result.TopLoser);
        Assert.Empty(result.Positions);

        _stockApiService.Verify(
            x => x.getPriceByTickerAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetPortfolioPerformanceSummaryAsync_CalculatesSingleHoldingCorrectly()
    {
        var service = CreateService();

        var userId = Guid.NewGuid();
        var portfolioId = Guid.NewGuid();

        var portfolio = CreatePortfolio(
            userId,
            portfolioId,
            "Growth Portfolio",
            100m,
            new List<Holding>
            {
                new Holding(portfolioId, "AAPL", 10m, 150m)
            });

        _portfolioService
            .Setup(x => x.UserOwnsPortfolio(
                userId,
                portfolioId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _portfolioService
            .Setup(x => x.getPortfolioEntityById(
                portfolioId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(portfolio);

        _stockApiService
            .Setup(x => x.getPriceByTickerAsync(
                "AAPL",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(180m);

        var result = await service.GetPortfolioPerformanceSummaryAsync(userId, portfolioId);

        Assert.Equal(1800m, result.HoldingsValue);
        Assert.Equal(1900m, result.TotalPortfolioValue);
        Assert.Equal(1500m, result.TotalCostBasis);
        Assert.Equal(300m, result.TotalGainLoss);
        Assert.Equal(20m, result.TotalGainLossPercent);

        Assert.Single(result.Positions);

        var position = result.Positions[0];

        Assert.Equal("AAPL", position.Ticker);
        Assert.Equal(10m, position.Quantity);
        Assert.Equal(150m, position.AverageBuyPrice);
        Assert.Equal(180m, position.CurrentPrice);
        Assert.Equal(1800m, position.MarketValue);
        Assert.Equal(1500m, position.CostBasis);
        Assert.Equal(300m, position.GainLoss);
        Assert.Equal(20m, position.GainLossPercent);

        Assert.Equal("AAPL", result.TopGainer?.Ticker);
        Assert.Equal("AAPL", result.TopLoser?.Ticker);
    }

    [Fact]
    public async Task GetPortfolioPerformanceSummaryAsync_FallsBackToAverageBuyPrice_WhenPriceIsNull()
    {
        var service = CreateService();

        var userId = Guid.NewGuid();
        var portfolioId = Guid.NewGuid();

        var portfolio = CreatePortfolio(
            userId,
            portfolioId,
            "Fallback Portfolio",
            0m,
            new List<Holding>
            {
                new Holding(portfolioId, "MSFT", 5m, 200m)
            });

        _portfolioService
            .Setup(x => x.UserOwnsPortfolio(
                userId,
                portfolioId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _portfolioService
            .Setup(x => x.getPortfolioEntityById(
                portfolioId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(portfolio);

        _stockApiService
            .Setup(x => x.getPriceByTickerAsync(
                "MSFT",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((decimal?)null);

        var result = await service.GetPortfolioPerformanceSummaryAsync(userId, portfolioId);

        Assert.Equal(1000m, result.HoldingsValue);
        Assert.Equal(1000m, result.TotalPortfolioValue);
        Assert.Equal(1000m, result.TotalCostBasis);
        Assert.Equal(0m, result.TotalGainLoss);
        Assert.Equal(0m, result.TotalGainLossPercent);

        Assert.Single(result.Positions);

        var position = result.Positions[0];

        Assert.Equal("MSFT", position.Ticker);
        Assert.Equal(200m, position.CurrentPrice);
        Assert.Equal(1000m, position.MarketValue);
        Assert.Equal(1000m, position.CostBasis);
        Assert.Equal(0m, position.GainLoss);
        Assert.Equal(0m, position.GainLossPercent);
    }

    [Fact]
    public async Task GetPortfolioPerformanceSummaryAsync_SelectsTopGainerAndTopLoser()
    {
        var service = CreateService();

        var userId = Guid.NewGuid();
        var portfolioId = Guid.NewGuid();

        var portfolio = CreatePortfolio(
            userId,
            portfolioId,
            "Mixed Portfolio",
            0m,
            new List<Holding>
            {
                new Holding(portfolioId, "AAPL", 10m, 100m),
                new Holding(portfolioId, "TSLA", 10m, 100m),
                new Holding(portfolioId, "MSFT", 10m, 100m)
            });

        _portfolioService
            .Setup(x => x.UserOwnsPortfolio(
                userId,
                portfolioId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _portfolioService
            .Setup(x => x.getPortfolioEntityById(
                portfolioId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(portfolio);

        _stockApiService
            .Setup(x => x.getPriceByTickerAsync(
                "AAPL",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(150m);

        _stockApiService
            .Setup(x => x.getPriceByTickerAsync(
                "TSLA",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(80m);

        _stockApiService
            .Setup(x => x.getPriceByTickerAsync(
                "MSFT",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(110m);

        var result = await service.GetPortfolioPerformanceSummaryAsync(userId, portfolioId);

        Assert.Equal(3400m, result.HoldingsValue);
        Assert.Equal(3000m, result.TotalCostBasis);
        Assert.Equal(400m, result.TotalGainLoss);
        Assert.Equal(13.33m, result.TotalGainLossPercent);

        Assert.Equal("AAPL", result.TopGainer?.Ticker);
        Assert.Equal(50m, result.TopGainer?.GainLossPercent);

        Assert.Equal("TSLA", result.TopLoser?.Ticker);
        Assert.Equal(-20m, result.TopLoser?.GainLossPercent);
    }
}