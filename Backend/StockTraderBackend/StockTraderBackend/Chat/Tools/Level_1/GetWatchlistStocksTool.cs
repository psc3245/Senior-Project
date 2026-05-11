using System.Text.Json;
using StockTraderBackend.Chat.LLMTools;
using StockTraderBackend.Watchlist;
using StockTraderBackend.Watchlist.Service;

namespace StockTraderBackend.Chat.Tools.Level_1;

public sealed class GetWatchlistStocksTool : ILlmTool
{
    private readonly IWatchlistService _watchlistService;
    private readonly IWatchlistStockService _watchlistStockService;

    public GetWatchlistStocksTool(
        IWatchlistService watchlistService,
        IWatchlistStockService watchlistStockService)
    {
        _watchlistService = watchlistService;
        _watchlistStockService = watchlistStockService;
    }

    public string Name => "get_watchlist_stocks";

    public string Description =>
        "Gets the stocks inside one of the authenticated user's watchlists, including ticker and company name.";

    public object JsonSchema => new
    {
        type = "object",
        properties = new
        {
            watchlistId = new
            {
                type = "string",
                description = "The watchlist ID."
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
        if (!arguments.TryGetProperty("watchlistId", out var idElement))
            return new { error = "watchlistId is required" };

        if (!Guid.TryParse(idElement.GetString(), out var watchlistId))
            return new { error = "watchlistId must be a valid GUID" };

        var watchlist = await _watchlistService.GetWatchlistById(watchlistId);

        if (watchlist == null)
            return new { error = "Watchlist not found" };

        if (watchlist.UserId != userId)
            return new { error = "Unauthorized access to watchlist" };

        var stocks = await _watchlistStockService.GetWatchlistStocks(watchlistId);

        return new
        {
            watchlistId = watchlist.Id,
            watchlistName = watchlist.Name,
            count = stocks.Count,
            stocks = stocks
                .OrderBy(s => s.Position)
                .Select(s => new
                {
                    symbolId = s.SymbolId,
                    ticker = s.Symbol?.Ticker,
                    name = s.Symbol?.Name,
                    exchange = s.Symbol?.Exchange,
                    assetType = s.Symbol?.AssetType.ToString(),
                    position = s.Position,
                    addedAt = s.AddedAt
                })
        };
    }
}