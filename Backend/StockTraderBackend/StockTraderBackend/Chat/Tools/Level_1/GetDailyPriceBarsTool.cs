using System.Text.Json;
using StockTraderBackend.Assets.PriceBar.Services;
using StockTraderBackend.Chat.LLMTools;

namespace StockTraderBackend.Chat.Tools.Level_1;

public sealed class GetDailyPriceBarsTool : ILlmTool
{
    private readonly IPriceBarsService _priceBarsService;

    public GetDailyPriceBarsTool(IPriceBarsService priceBarsService)
    {
        _priceBarsService = priceBarsService;
    }

    public string Name => "get_daily_price_bars";

    public string Description =>
        "Gets historical daily price bars for a ticker, including open, high, low, close, volume, and timestamps.";

    public object JsonSchema => new
    {
        type = "object",
        properties = new
        {
            ticker = new
            {
                type = "string",
                description = "Ticker symbol, such as AAPL, MSFT, TSLA, or NVDA."
            },
            from = new
            {
                type = "string",
                description = "Optional start date in YYYY-MM-DD format."
            },
            to = new
            {
                type = "string",
                description = "Optional end date in YYYY-MM-DD format."
            },
            limit = new
            {
                type = "integer",
                description = "Maximum number of daily bars to return. Defaults to 30 and is capped at 100."
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

        DateOnly? from = null;
        DateOnly? to = null;
        var limit = 30;

        if (arguments.TryGetProperty("from", out var fromElement) &&
            DateOnly.TryParse(fromElement.GetString(), out var parsedFrom))
        {
            from = parsedFrom;
        }

        if (arguments.TryGetProperty("to", out var toElement) &&
            DateOnly.TryParse(toElement.GetString(), out var parsedTo))
        {
            to = parsedTo;
        }

        if (arguments.TryGetProperty("limit", out var limitElement) &&
            limitElement.TryGetInt32(out var parsedLimit))
        {
            limit = Math.Clamp(parsedLimit, 1, 100);
        }

        var response = await _priceBarsService.GetDailyBarsAsync(
            ticker,
            "1d",
            from,
            to,
            limit,
            ct
        );

        return response;
    }
}