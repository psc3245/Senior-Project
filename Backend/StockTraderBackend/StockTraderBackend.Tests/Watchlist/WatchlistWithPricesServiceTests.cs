using Moq;
using StockTraderBackend.Assets.Symbols.Model;
using StockTraderBackend.Middleware;
using StockTraderBackend.StockAPI;
using StockTraderBackend.Watchlist;
using StockTraderBackend.Watchlist.Service;
using StockTraderBackend.Watchlists.Service;

namespace StockTraderBackend.Tests.Watchlist;

public sealed class WatchlistWithPricesServiceTests
{
    private readonly Mock<IWatchlistService> _watchlistService = new();
    private readonly Mock<IStockAPIService> _stockApiService = new();

    private WatchlistWithPricesService CreateService()
    {
        return new WatchlistWithPricesService(
            _watchlistService.Object,
            _stockApiService.Object);
    }

    private static WatchlistStock CreateWatchlistStock(
        Guid watchlistId,
        int symbolId,
        string ticker,
        string name,
        int position)
    {
        return new WatchlistStock
        {
            WatchlistId = watchlistId,
            SymbolId = symbolId,
            Position = position,
            AddedAt = DateTime.UtcNow,
            Symbol = new Symbol
            {
                Id = symbolId,
                Ticker = ticker,
                Name = name
            }
        };
    }

    [Fact]
    public async Task GetWatchlistWithPricesAsync_Throws403_WhenUserDoesNotOwnWatchlist()
    {
        var service = CreateService();

        var userId = Guid.NewGuid();
        var watchlistId = Guid.NewGuid();

        _watchlistService
            .Setup(x => x.UserOwnsWatchlist(
                userId,
                watchlistId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var ex = await Assert.ThrowsAsync<ApiException>(() =>
            service.GetWatchlistWithPricesAsync(userId, watchlistId));

        Assert.Equal(403, ex.StatusCode);

        _watchlistService.Verify(
            x => x.GetWatchlistById(
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetWatchlistWithPricesAsync_Throws404_WhenWatchlistDoesNotExist()
    {
        var service = CreateService();

        var userId = Guid.NewGuid();
        var watchlistId = Guid.NewGuid();

        _watchlistService
            .Setup(x => x.UserOwnsWatchlist(
                userId,
                watchlistId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _watchlistService
            .Setup(x => x.GetWatchlistById(
                watchlistId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((StockTraderBackend.Watchlist.Watchlist?)null);

        var ex = await Assert.ThrowsAsync<ApiException>(() =>
            service.GetWatchlistWithPricesAsync(userId, watchlistId));

        Assert.Equal(404, ex.StatusCode);
    }

    [Fact]
    public async Task GetWatchlistWithPricesAsync_ReturnsEmptyList_WhenWatchlistHasNoStocks()
    {
        var service = CreateService();

        var userId = Guid.NewGuid();
        var watchlistId = Guid.NewGuid();

        var watchlist = new StockTraderBackend.Watchlist.Watchlist
        {
            Id = watchlistId,
            UserId = userId,
            Name = "Tech",
            CreatedAt = DateTime.UtcNow,
            Stocks = new List<WatchlistStock>()
        };

        _watchlistService
            .Setup(x => x.UserOwnsWatchlist(
                userId,
                watchlistId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _watchlistService
            .Setup(x => x.GetWatchlistById(
                watchlistId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(watchlist);

        var result = await service.GetWatchlistWithPricesAsync(userId, watchlistId);

        Assert.Equal(watchlistId, result.WatchlistId);
        Assert.Equal("Tech", result.Name);
        Assert.Equal(0, result.Count);
        Assert.Empty(result.Stocks);

        _stockApiService.Verify(
            x => x.getPriceByTickerAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetWatchlistWithPricesAsync_ReturnsStocksWithPrices()
    {
        var service = CreateService();

        var userId = Guid.NewGuid();
        var watchlistId = Guid.NewGuid();

        var watchlist = new StockTraderBackend.Watchlist.Watchlist
        {
            Id = watchlistId,
            UserId = userId,
            Name = "Tech",
            CreatedAt = DateTime.UtcNow,
            Stocks = new List<WatchlistStock>
            {
                CreateWatchlistStock(
                    watchlistId,
                    1,
                    "AAPL",
                    "Apple Inc.",
                    1),

                CreateWatchlistStock(
                    watchlistId,
                    2,
                    "MSFT",
                    "Microsoft Corporation",
                    2)
            }
        };

        _watchlistService
            .Setup(x => x.UserOwnsWatchlist(
                userId,
                watchlistId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _watchlistService
            .Setup(x => x.GetWatchlistById(
                watchlistId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(watchlist);

        _stockApiService
            .Setup(x => x.getPriceByTickerAsync(
                "AAPL",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(280.14m);

        _stockApiService
            .Setup(x => x.getPriceByTickerAsync(
                "MSFT",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(510.22m);

        var result = await service.GetWatchlistWithPricesAsync(userId, watchlistId);

        Assert.Equal(watchlistId, result.WatchlistId);
        Assert.Equal("Tech", result.Name);
        Assert.Equal(2, result.Count);

        Assert.Equal("AAPL", result.Stocks[0].Ticker);
        Assert.Equal("Apple Inc.", result.Stocks[0].Name);
        Assert.Equal(280.14m, result.Stocks[0].CurrentPrice);

        Assert.Equal("MSFT", result.Stocks[1].Ticker);
        Assert.Equal("Microsoft Corporation", result.Stocks[1].Name);
        Assert.Equal(510.22m, result.Stocks[1].CurrentPrice);
    }

    [Fact]
    public async Task GetWatchlistWithPricesAsync_OrdersStocksByPosition()
    {
        var service = CreateService();

        var userId = Guid.NewGuid();
        var watchlistId = Guid.NewGuid();

        var watchlist = new StockTraderBackend.Watchlist.Watchlist
        {
            Id = watchlistId,
            UserId = userId,
            Name = "Tech",
            CreatedAt = DateTime.UtcNow,
            Stocks = new List<WatchlistStock>
            {
                CreateWatchlistStock(
                    watchlistId,
                    1,
                    "AAPL",
                    "Apple Inc.",
                    2),

                CreateWatchlistStock(
                    watchlistId,
                    2,
                    "MSFT",
                    "Microsoft Corporation",
                    1)
            }
        };

        _watchlistService
            .Setup(x => x.UserOwnsWatchlist(
                userId,
                watchlistId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _watchlistService
            .Setup(x => x.GetWatchlistById(
                watchlistId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(watchlist);

        _stockApiService
            .Setup(x => x.getPriceByTickerAsync(
                "AAPL",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(280.14m);

        _stockApiService
            .Setup(x => x.getPriceByTickerAsync(
                "MSFT",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(510.22m);

        var result = await service.GetWatchlistWithPricesAsync(userId, watchlistId);

        Assert.Equal("MSFT", result.Stocks[0].Ticker);
        Assert.Equal("AAPL", result.Stocks[1].Ticker);
    }

    [Fact]
    public async Task GetWatchlistWithPricesAsync_Throws404_WhenPriceIsMissing()
    {
        var service = CreateService();

        var userId = Guid.NewGuid();
        var watchlistId = Guid.NewGuid();

        var watchlist = new StockTraderBackend.Watchlist.Watchlist
        {
            Id = watchlistId,
            UserId = userId,
            Name = "Tech",
            CreatedAt = DateTime.UtcNow,
            Stocks = new List<WatchlistStock>
            {
                CreateWatchlistStock(
                    watchlistId,
                    1,
                    "AAPL",
                    "Apple Inc.",
                    1)
            }
        };

        _watchlistService
            .Setup(x => x.UserOwnsWatchlist(
                userId,
                watchlistId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _watchlistService
            .Setup(x => x.GetWatchlistById(
                watchlistId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(watchlist);

        _stockApiService
            .Setup(x => x.getPriceByTickerAsync(
                "AAPL",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((decimal?)null);

        var ex = await Assert.ThrowsAsync<ApiException>(() =>
            service.GetWatchlistWithPricesAsync(userId, watchlistId));

        Assert.Equal(404, ex.StatusCode);
    }
}