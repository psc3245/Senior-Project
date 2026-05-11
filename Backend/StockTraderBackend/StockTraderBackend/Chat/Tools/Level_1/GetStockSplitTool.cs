using System.Text.Json;
using StockTraderBackend.Chat.LLMTools;
using StockTraderBackend.MarketData.StockSplits.Service;

namespace StockTraderBackend.Chat.Tools.Level_1;

public sealed class GetStockSplitsTool : ILlmTool
{
    private readonly IStockSplitDetectionService _stockSplitService;

    public GetStockSplitsTool(IStockSplitDetectionService stockSplitService)
    {
        _stockSplitService = stockSplitService;
    }

    public string Name => "get_stock_splits";

    public string Description =>
        "Gets stored stock split records from the database. Use this when the user asks about recent stock splits, split history for a ticker, or why a stock may have changed price due to a split. This tool only reads already-stored splits and does not scan or modify data.";

    public object JsonSchema => new
    {
        type = "object",
        properties = new
        {
            ticker = new
            {
                type = "string",
                description = "Optional ticker symbol, such as AAPL, TSLA, NVDA, or BKNG. If provided, returns splits for that ticker."
            },
            lookbackDays = new
            {
                type = "integer",
                description = "Optional number of days to look back for recent stored splits. Defaults to 30 and is capped at 365."
            },
            limit = new
            {
                type = "integer",
                description = "Maximum number of split records to return. Defaults to 25 and is capped at 100."
            }
        },
        additionalProperties = false
    };

    public async Task<object> ExecuteAsync(
        Guid userId,
        JsonElement arguments,
        CancellationToken ct)
    {
        string? ticker = null;
        int lookbackDays = 30;
        int limit = 25;

        if (arguments.TryGetProperty("ticker", out var tickerElement))
        {
            ticker = tickerElement.GetString()?.Trim().ToUpperInvariant();
        }

        if (arguments.TryGetProperty("lookbackDays", out var lookbackElement) &&
            lookbackElement.TryGetInt32(out var parsedLookback))
        {
            lookbackDays = Math.Clamp(parsedLookback, 1, 365);
        }

        if (arguments.TryGetProperty("limit", out var limitElement) &&
            limitElement.TryGetInt32(out var parsedLimit))
        {
            limit = Math.Clamp(parsedLimit, 1, 100);
        }

        var splits = !string.IsNullOrWhiteSpace(ticker)
            ? await _stockSplitService.GetSplitsForTickerAsync(ticker, ct)
            : await _stockSplitService.GetRecentStoredSplitsAsync(lookbackDays, limit, ct);

        return new
        {
            ticker,
            lookbackDays = string.IsNullOrWhiteSpace(ticker) ? lookbackDays : (int?)null,
            count = splits.Count,
            splits = splits
                .Take(limit)
                .Select(s => new
                {
                    splitId = s.Id,
                    ticker = s.Symbol?.Ticker,
                    symbolId = s.SymbolId,
                    effectiveDate = s.EffectiveDate,
                    splitFrom = s.SplitFrom,
                    splitTo = s.SplitTo,
                    ratio = $"{s.SplitTo}:{s.SplitFrom}",
                    massiveSplitId = s.MassiveSplitId
                })
        };
    }
}