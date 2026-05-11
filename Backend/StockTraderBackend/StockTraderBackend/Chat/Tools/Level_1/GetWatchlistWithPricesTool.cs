using System.Text.Json;
using StockTraderBackend.Chat.LLMTools;
using StockTraderBackend.Watchlist.Service;

namespace StockTraderBackend.Chat.Tools.Level_1;

public sealed class GetWatchlistWithPricesTool : ILlmTool
{
    private readonly IWatchlistWithPricesService _watchlistWithPricesService;

    public GetWatchlistWithPricesTool(
        IWatchlistWithPricesService watchlistWithPricesService)
    {
        _watchlistWithPricesService = watchlistWithPricesService;
    }

    public string Name => "get_watchlist_with_prices";

    public string Description =>
        "Gets one of the user's watchlists with the latest available price for each stock. Use this when the user asks about their watchlist, watched stocks, current prices in a watchlist, or wants to review a watchlist with prices.";

    public object JsonSchema => new
    {
        type = "object",
        properties = new
        {
            watchlistId = new
            {
                type = "string",
                description = "The watchlist ID as a GUID."
            }
        },
        required = new[] { "watchlistId" },
        additionalProperties = false
    };

    public async Task<object> ExecuteAsync(
        Guid userId,
        JsonElement arguments,
        CancellationToken ct)
    {
        if (!arguments.TryGetProperty("watchlistId", out var watchlistIdElement))
        {
            return new { error = "watchlistId is required" };
        }

        var watchlistIdText = watchlistIdElement.GetString();

        if (!Guid.TryParse(watchlistIdText, out var watchlistId))
        {
            return new { error = "watchlistId must be a valid GUID" };
        }

        var response = await _watchlistWithPricesService.GetWatchlistWithPricesAsync(
            userId,
            watchlistId,
            ct);

        return response;
    }
}