using System.Text.Json;
using StockTraderBackend.Chat.LLMTools;
using StockTraderBackend.StockAPI;

namespace StockTraderBackend.Chat.Tools.Level_1;

public sealed class GetCurrentStockPriceTool : ILlmTool
{
    private readonly IStockAPIService _stockApiService;

    public GetCurrentStockPriceTool(IStockAPIService stockApiService)
    {
        _stockApiService = stockApiService;
    }

    public string Name => "get_current_stock_price";

    public string Description =>
        "Gets the latest available stock price for a ticker symbol.";

    public object JsonSchema => new
    {
        type = "object",
        properties = new
        {
            ticker = new
            {
                type = "string",
                description = "Ticker symbol, such as AAPL, MSFT, TSLA, or NVDA."
            }
        },
        required = new[] { "ticker" },
        additionalProperties = false
    };

    public async Task<object> ExecuteAsync(
        Guid userId,
        JsonElement arguments,
        CancellationToken ct)
    {
        if (!arguments.TryGetProperty("ticker", out var tickerElement))
            return new { error = "ticker is required" };

        var ticker = tickerElement.GetString()?.Trim().ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(ticker))
            return new { error = "ticker is required" };

        var price = await _stockApiService.getPriceByTickerAsync(ticker, ct);

        return new
        {
            ticker,
            price,
            found = price.HasValue
        };
    }
}