using System.Text.Json;
using StockTraderBackend.Chat.LLMTools;
using StockTraderBackend.News.Services;

namespace StockTraderBackend.Chat.Tools.Level_1;

public sealed class GetFavoriteArticlesTool : ILlmTool
{
    private readonly IFavoriteArticleService _favoriteArticleService;

    public GetFavoriteArticlesTool(IFavoriteArticleService favoriteArticleService)
    {
        _favoriteArticleService = favoriteArticleService;
    }

    public string Name => "get_favorite_articles";

    public string Description =>
        "Gets articles the authenticated user has favorited or saved.";

    public object JsonSchema => new
    {
        type = "object",
        properties = new
        {
            max = new
            {
                type = "integer",
                description = "Maximum number of favorite articles to return. Defaults to 10 and is capped at 25."
            }
        },
        additionalProperties = false
    };

    public async Task<object> ExecuteAsync(
        Guid userId,
        JsonElement arguments,
        CancellationToken ct)
    {
        var max = 10;

        if (arguments.TryGetProperty("max", out var maxElement) &&
            maxElement.TryGetInt32(out var parsedMax))
        {
            max = Math.Clamp(parsedMax, 1, 25);
        }

        var articles = await _favoriteArticleService.GetFavoritesAsync(userId);

        return new
        {
            count = articles.Count,
            articles = articles
                .Take(max)
                .Select(a => new
                {
                    a.Id,
                    a.Title,
                    a.Description,
                    a.Url,
                    a.PublishedAt,
                    a.SourceName,
                    a.Favorited
                })
        };
    }
}