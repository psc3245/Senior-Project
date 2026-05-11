using System.Text.Json;
using StockTraderBackend.Chat.LLMTools;
using StockTraderBackend.Portfolios.Performance;
using StockTraderBackend.Portfolios.Services;

namespace StockTraderBackend.Chat.Tools.Level_1;

public sealed class GetPortfolioPerformanceSummaryTool : ILlmTool
{
    private readonly IPortfolioPerformanceService _portfolioPerformanceService;

    public GetPortfolioPerformanceSummaryTool(
        IPortfolioPerformanceService portfolioPerformanceService)
    {
        _portfolioPerformanceService = portfolioPerformanceService;
    }

    public string Name => "get_portfolio_performance_summary";

    public string Description =>
        "Gets the current performance summary for one of the user's portfolios, including cash balance, holdings value, total portfolio value, total unrealized gain/loss, gain/loss percent, top gainer, top loser, and position-level performance. Use this when the user asks how their portfolio is doing, how much money they have made or lost, their best or worst holding, or their current portfolio value.";

    public object JsonSchema => new
    {
        type = "object",
        properties = new
        {
            portfolioId = new
            {
                type = "string",
                description = "The portfolio ID as a GUID."
            }
        },
        required = new[] { "portfolioId" },
        additionalProperties = false
    };

    public async Task<object> ExecuteAsync(
        Guid userId,
        JsonElement arguments,
        CancellationToken ct)
    {
        if (!arguments.TryGetProperty("portfolioId", out var portfolioIdElement))
        {
            return new { error = "portfolioId is required" };
        }

        var portfolioIdText = portfolioIdElement.GetString();

        if (!Guid.TryParse(portfolioIdText, out var portfolioId))
        {
            return new { error = "portfolioId must be a valid GUID" };
        }

        var response = await _portfolioPerformanceService
            .GetPortfolioPerformanceSummaryAsync(userId, portfolioId, ct);

        return response;
    }
}