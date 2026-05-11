using System.Text.Json;
using StockTraderBackend.Chat.LLMTools;
using StockTraderBackend.Watchlist;

namespace StockTraderBackend.Chat.Tools.Level_1;

public sealed class GetUserWatchlistsTool : ILlmTool
{
    private readonly IWatchlistService _watchlistService;

    public GetUserWatchlistsTool(IWatchlistService watchlistService)
    {
        _watchlistService = watchlistService;
    }

    public string Name => "get_user_watchlists";

    public string Description =>
        "Gets the authenticated user's watchlists.";

    public object JsonSchema => new
    {
        type = "object",
        properties = new { },
        additionalProperties = false
    };

    public async Task<object> ExecuteAsync(
        Guid userId,
        JsonElement arguments,
        CancellationToken ct)
    {
        var watchlists = await _watchlistService.GetUserWatchlists(userId);

        return new
        {
            count = watchlists.Count,
            watchlists = watchlists.Select(w => new
            {
                watchlistId = w.Id,
                name = w.Name,
                createdAt = w.CreatedAt,
                stockCount = w.Stocks?.Count ?? 0
            })
        };
    }
}