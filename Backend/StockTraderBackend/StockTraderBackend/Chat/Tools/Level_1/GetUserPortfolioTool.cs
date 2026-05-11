using System.Text.Json;
using StockTraderBackend.Chat.LLMTools;
using StockTraderBackend.Portfolios;

namespace StockTraderBackend.Chat.Tools.Level_1;

public sealed class GetUserPortfolioTool : ILlmTool
{
    private readonly IPortfolioService _portfolioService;

    public GetUserPortfolioTool(IPortfolioService portfolioService)
    {
        _portfolioService = portfolioService;
    }

    public string Name => "get_user_portfolio";

    public string Description =>
        "Gets the authenticated user's portfolio, including cash balance, trade history and holdings.";

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
        var portfolio = await _portfolioService.getPortfolioByUserId(userId, ct);

        return portfolio;
    }
}