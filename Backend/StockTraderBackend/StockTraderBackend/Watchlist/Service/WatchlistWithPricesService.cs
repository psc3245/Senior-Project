using StockTraderBackend.Middleware;
using StockTraderBackend.StockAPI;
using StockTraderBackend.Watchlist.DTOs;
using StockTraderBackend.Watchlist.Service;

namespace StockTraderBackend.Watchlists.Service;

public sealed class WatchlistWithPricesService : IWatchlistWithPricesService
{
    private readonly IWatchlistService _watchlistService;
    private readonly IStockAPIService _stockApiService;

    public WatchlistWithPricesService(
        IWatchlistService watchlistService,
        IStockAPIService stockApiService)
    {
        _watchlistService = watchlistService;
        _stockApiService = stockApiService;
    }

    public async Task<WatchlistWithPricesDto> GetWatchlistWithPricesAsync(
        Guid userId,
        Guid watchlistId,
        CancellationToken ct = default)
    {
        var ownsWatchlist = await _watchlistService.UserOwnsWatchlist(
            userId,
            watchlistId,
            ct);

        if (!ownsWatchlist)
        {
            throw new ApiException(403, "User does not own this watchlist.");
        }

        var watchlist = await _watchlistService.GetWatchlistById(
            watchlistId);

        if (watchlist == null)
        {
            throw new ApiException(404, $"Watchlist '{watchlistId}' not found.");
        }

        var stocks = new List<WatchlistStockWithPriceDto>();

        foreach (var stock in watchlist.Stocks.OrderBy(s => s.Position))
        {
            var ticker = stock.Symbol.Ticker.Trim().ToUpperInvariant();

            var currentPrice = await _stockApiService.getPriceByTickerAsync(
                ticker,
                ct);

            if (currentPrice == null)
            {
                throw new ApiException(404, $"No price found for ticker '{ticker}'.");
            }

            stocks.Add(new WatchlistStockWithPriceDto
            {
                Ticker = ticker,
                Name = stock.Symbol.Name,
                CurrentPrice = Math.Round(currentPrice.Value, 2)
            });
        }

        return new WatchlistWithPricesDto
        {
            WatchlistId = watchlist.Id,
            Name = watchlist.Name,
            Count = stocks.Count,
            Stocks = stocks
        };
    }
}