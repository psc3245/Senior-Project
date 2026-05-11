using System.Text.Json;
using StockTraderBackend.Assets.Symbols.Service;
using StockTraderBackend.Chat.LLMTools;

namespace StockTraderBackend.Chat.Tools.Level_1;

public sealed class GetRandomSymbolsTool : ILlmTool
{
    private readonly ISymbolsService _symbolsService;

    public GetRandomSymbolsTool(ISymbolsService symbolsService)
    {
        _symbolsService = symbolsService;
    }

    public string Name => "get_random_symbols";

    public string Description =>
        "Gets a random sample of active tradeable symbols. Use this when the user asks for examples of stocks or available companies. This should be prioritized over list symbols.";

    public object JsonSchema => new
    {
        type = "object",
        properties = new
        {
            count = new
            {
                type = "integer",
                description = "Number of symbols to return (1 to 100). Defaults to 25 if not specified."
            }
        },
        additionalProperties = false
    };

    public async Task<object> ExecuteAsync(
        Guid userId,
        JsonElement arguments,
        CancellationToken ct)
    {
        int count = 25; // default

        if (arguments.TryGetProperty("count", out var countElement) &&
            countElement.TryGetInt32(out var parsed))
        {
            count = parsed;
        }

        var symbols = await _symbolsService.GetRandomSubsetOfSymbols(count, ct);

        return new
        {
            count = symbols.Count,
            symbols = symbols.Select(s => new
            {
                ticker = s.Ticker,
                name = s.Name,
                exchange = s.Exchange,
                assetType = s.AssetType.ToString()
            })
        };
    }
}