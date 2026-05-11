using System.Text.Json;
using StockTraderBackend.Assets.Symbols.Service;
using StockTraderBackend.Chat.LLMTools;

namespace StockTraderBackend.Chat.Tools.Level_1;

public sealed class GetSymbolTool : ILlmTool
{
    private readonly ISymbolsService _symbolsService;

    public GetSymbolTool(ISymbolsService symbolsService)
    {
        _symbolsService = symbolsService;
    }

    public string Name => "get_symbol";

    public string Description =>
        "Looks up a stock or asset symbol by ticker or company name.";

    public object JsonSchema => new
    {
        type = "object",
        properties = new
        {
            ticker = new
            {
                type = "string",
                description = "Ticker symbol, such as AAPL, MSFT, TSLA, or BA."
            },
            companyName = new
            {
                type = "string",
                description = "Company name, such as Apple, Microsoft, Tesla, or Boeing."
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
        string? companyName = null;

        if (arguments.TryGetProperty("ticker", out var tickerElement))
            ticker = tickerElement.GetString();

        if (arguments.TryGetProperty("companyName", out var companyElement))
            companyName = companyElement.GetString();

        if (!string.IsNullOrWhiteSpace(ticker))
        {
            var symbol = await _symbolsService.GetSymbolByTickerAsync(
                ticker.Trim().ToUpperInvariant(),
                ct
            );

            return new { symbol };
        }

        if (!string.IsNullOrWhiteSpace(companyName))
        {
            var symbol = await _symbolsService.GetSymbolByCompanyNameAsync(
                companyName.Trim(),
                ct
            );

            return new { symbol };
        }

        return new
        {
            error = "Either ticker or companyName is required."
        };
    }
}