using System.Text.Json;
using StockTraderBackend.Chat.LLMTools;
using StockTraderBackend.News.Services;

namespace StockTraderBackend.Chat.Tools.Level_1;

public sealed class SearchArticlesTool : ILlmTool
{
    private readonly INewsService _newsService;

    public SearchArticlesTool(INewsService newsService)
    {
        _newsService = newsService;
    }

    public string Name => "search_articles";

    public string Description =>
        "Searches news articles by query, ticker, company name, topic, or keyword. Use sparingly, only when a user prompt requests a search should you use this tool.";

    public object JsonSchema => new
    {
        type = "object",
        properties = new
        {
            query = new
            {
                type = "string",
                description = "Search query such as AAPL, Apple, Tesla earnings, market news, or inflation."
            },
            max = new
            {
                type = "integer",
                description = "Maximum number of articles to return. Defaults to 5 and is capped at 20."
            }
        },
        required = new[] { "query" },
        additionalProperties = false
    };

    public async Task<object> ExecuteAsync(
        Guid userId,
        JsonElement arguments,
        CancellationToken ct)
    {
        if (!arguments.TryGetProperty("query", out var queryElement))
            return new { error = "query is required" };

        var query = queryElement.GetString()?.Trim();

        if (string.IsNullOrWhiteSpace(query))
            return new { error = "query is required" };

        var max = 5;

        if (arguments.TryGetProperty("max", out var maxElement) &&
            maxElement.TryGetInt32(out var parsedMax))
        {
            max = Math.Clamp(parsedMax, 1, 20);
        }

        var articles = await _newsService.SearchArticlesAsync(
            q: query,
            lang: "en",
            country: "us",
            page: 1,
            max: max,
            userId: userId
        );

        return new
        {
            query,
            count = articles.Count,
            articles = articles.Select(a => new
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