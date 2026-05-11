using StockTraderBackend.Chat.LLMTools;
using StockTraderBackend.Portfolios;
using StockTraderBackend.PortfolioValueTracking;
using System.Text.Json;

namespace StockTraderBackend.Chat.Tools.Level_1;

public sealed class GetPortfolioSnapshotsTool : ILlmTool
{
    private readonly IPortfolioService _portfolioService;
    private readonly IPortfolioSnapshotQueryService _snapshotService;

    public GetPortfolioSnapshotsTool(
        IPortfolioService portfolioService,
        IPortfolioSnapshotQueryService snapshotService)
    {
        _portfolioService = portfolioService;
        _snapshotService = snapshotService;
    }
    public string Name => "get_portfolio_snapshots";

    public string Description =>
        "Gets historical portfolio value snapshots for a portfolio, including total value, percent change, and per-holding values over time. " +
        "Use this when the user asks about portfolio performance, history, trends, or how their portfolio has changed.";

    public object JsonSchema => new
    {
        type = "object",
        properties = new
        {
            portfolioId = new
            {
                type = "string",
                description = "The portfolio ID to retrieve historical snapshots for."
            },
            from = new
            {
                type = "string",
                description = "Optional start date in YYYY-MM-DD format."
            },
            to = new
            {
                type = "string",
                description = "Optional end date in YYYY-MM-DD format."
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
        if (!arguments.TryGetProperty("portfolioId", out var idElement))
            return new { error = "portfolioId is required" };

        if (!Guid.TryParse(idElement.GetString(), out var portfolioId))
            return new { error = "portfolioId must be a valid GUID" };

        bool ownsPortfolio;

        try
        {
            ownsPortfolio = await _portfolioService.UserOwnsPortfolio(
                userId,
                portfolioId,
                ct
            );
        }
        catch (KeyNotFoundException)
        {
            return new { error = "Portfolio not found" };
        }

        if (!ownsPortfolio)
            return new { error = "Unauthorized access to portfolio" };

        DateOnly? from = null;
        DateOnly? to = null;

        if (arguments.TryGetProperty("from", out var fromElement) &&
            DateOnly.TryParse(fromElement.GetString(), out var parsedFrom))
        {
            from = parsedFrom;
        }

        if (arguments.TryGetProperty("to", out var toElement) &&
            DateOnly.TryParse(toElement.GetString(), out var parsedTo))
        {
            to = parsedTo;
        }

        var response = await _snapshotService.GetSnapshotsForPortfolioAsync(
            portfolioId,
            from,
            to,
            ct
        );

        return new
        {
            portfolioId = response.PortfolioId,
            snapshotCount = response.Data.Count,
            snapshots = response.Data.Select(s => new
            {
                date = s.Date,
                totalValue = s.TotalValue,
                portfolioChangePercent = s.PortfolioChangePercent,
                holdings = s.Holdings.Select(h => new
                {
                    ticker = h.Ticker,
                    shares = h.Shares,
                    priceAtSnapshot = h.PriceAtSnapshot,
                    value = h.Value,
                    priceChangePercent = h.PriceChangePercent
                })
            })
        };
    }
}