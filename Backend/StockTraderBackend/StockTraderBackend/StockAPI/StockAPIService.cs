namespace StockTraderBackend.StockAPI;

using StockTraderBackend.Assets.PriceBar;
public class StockAPIService : IStockAPIService
{
    private readonly IPriceBarsRepository _priceBars;

    public StockAPIService(IPriceBarsRepository priceBars)
    {
        _priceBars = priceBars;
    }

    public async Task<decimal?> getPriceByTickerAsync(
        string ticker,
        CancellationToken ct = default)
    {
        return await _priceBars
            .GetLatestCloseByTickerAsync(ticker, "1d", ct);
    }
    public async Task<Dictionary<string, decimal>> GetPricesByTickersAsync(
        IEnumerable<string> tickers,
        CancellationToken ct = default)
    {
        return await _priceBars
            .GetLatestClosesByTickersAsync(tickers, "1d", ct);
    }
}
