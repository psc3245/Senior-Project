using System.Text.Json;
using StockTraderBackend.Assets.Symbols.Service;
using StockTraderBackend.Chat.LLMTools;

namespace StockTraderBackend.Chat.Tools.Level_1;

public sealed class ListSymbolsTool : ILlmTool
{
    private readonly ISymbolsService _symbolsService;

    public ListSymbolsTool(ISymbolsService symbolsService)
    {
        _symbolsService = symbolsService;
    }

    public string Name => "list_symbols";

    public string Description =>
        "Lists available stock or asset symbols in the app. Use this only in rare occasions where user asks for alphabetically sorted symbols. This should be used significantly less than \"get_random_symbols\".";

    public object JsonSchema => new
    {
        type = "object",
        properties = new
        {
            limit = new
            {
                type = "integer",
                description = "Maximum number of symbols to return. Default is 25."
            }
        },
        additionalProperties = false
    };

    public async Task<object> ExecuteAsync(
        Guid userId,
        JsonElement arguments,
        CancellationToken ct)
    {
        var limit = 25;

        if (arguments.TryGetProperty("limit", out var limitElement) &&
            limitElement.TryGetInt32(out var parsedLimit))
        {
            limit = Math.Clamp(parsedLimit, 1, 100);
        }

        var symbols = await _symbolsService.GetAllSymbolsAsync(ct);

        return new
        {
            count = symbols.Count,
            symbols = symbols
                .Where(s => s.IsActive)
                .OrderBy(s => s.Ticker)
                .Take(limit)
                .Select(s => new
                {
                    id = s.Id,
                    ticker = s.Ticker,
                    name = s.Name,
                    exchange = s.Exchange,
                    assetType = s.AssetType.ToString()
                })
        };
    }
}