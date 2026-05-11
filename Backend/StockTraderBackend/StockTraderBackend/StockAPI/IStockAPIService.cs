
namespace StockTraderBackend.StockAPI
{
    public interface IStockAPIService
    {
        Task<decimal?> getPriceByTickerAsync(string ticker, CancellationToken ct = default);
        Task<Dictionary<string, decimal>> GetPricesByTickersAsync(IEnumerable<string> tickers, CancellationToken ct = default);
    }
}