using System.Text.Json;
using StockTraderBackend.Chat.LLMTools;
using StockTraderBackend.StockAPI;

namespace StockTraderBackend.Chat.Tools.Level_1;

public sealed class GetPricesForTickersTool : ILlmTool
{
    private readonly IStockAPIService _stockApiService;

    public GetPricesForTickersTool(IStockAPIService stockApiService)
    {
        _stockApiService = stockApiService;
    }

    public string Name => "get_prices_for_tickers";

    public string Description =>
        "Gets the latest available stock prices for multiple ticker symbols. Use this when the user asks for current prices, wants to compare several tickers, or provides a list of stocks.";

    public object JsonSchema => new
    {
        type = "object",
        properties = new
        {
            tickers = new
            {
                type = "array",
                description = "Ticker symbols to price, such as AAPL, MSFT, TSLA, or NVDA. Maximum 20 tickers.",
                items = new
                {
                    type = "string"
                }
            }
        },
        required = new[] { "tickers" },
        additionalProperties = false
    };

    public async Task<object> ExecuteAsync(
        Guid userId,
        JsonElement arguments,
        CancellationToken ct)
    {
        if (!arguments.TryGetProperty("tickers", out var tickersElement) ||
            tickersElement.ValueKind != JsonValueKind.Array)
        {
            return new { error = "tickers is required and must be an array" };
        }

        var tickers = tickersElement
            .EnumerateArray()
            .Where(x => x.ValueKind == JsonValueKind.String)
            .Select(x => x.GetString()?.Trim().ToUpperInvariant())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct()
            .Take(20)
            .ToList();

        if (tickers.Count == 0)
        {
            return new { error = "At least one valid ticker is required" };
        }

        var prices = await _stockApiService.GetPricesByTickersAsync(
            tickers!,
            ct);

        return tickers.Select(ticker => new
        {
            Ticker = ticker,
            CurrentPrice = prices.TryGetValue(ticker, out var price)
                ? Math.Round(price, 2)
                : (decimal?)null
        }).ToList();
    }
}