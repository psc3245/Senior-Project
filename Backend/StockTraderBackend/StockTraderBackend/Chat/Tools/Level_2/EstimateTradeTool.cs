using System.Text.Json;
using StockTraderBackend.Chat.LLMTools;
using StockTraderBackend.Middleware;
using StockTraderBackend.Trades;
using StockTraderBackend.Trades.Services;

namespace StockTraderBackend.Chat.Tools.Level_2;

public sealed class EstimateTradeTool : ILlmTool
{
    private readonly ITradeEstimateService _tradeEstimateService;

    public EstimateTradeTool(ITradeEstimateService tradeEstimateService)
    {
        _tradeEstimateService = tradeEstimateService;
    }

    public string Name => "estimate_trade";

    public string Description =>
        "Estimates a buy or sell trade without executing it, using the current user's default portfolio automatically. Use this when the user asks what would happen if they bought or sold a stock, whether they can afford a trade, or wants a trade preview. This tool does not modify the portfolio. Fractional share quantities are supported. When summarizing the result, include the RequiredConfirmation phrase exactly if CanExecute is true. Do not ask the user for a portfolio ID.";
    
    public object JsonSchema => new
    {
        type = "object",
        properties = new
        {
            action = new
            {
                type = "string",
                description = "The trade action. Must be BUY or SELL.",
                @enum = new[] { "BUY", "SELL" }
            },
            ticker = new
            {
                type = "string",
                description = "Ticker symbol, such as AAPL, MSFT, TSLA, or NVDA."
            },
            quantity = new
            {
                type = "number",
                description = "Number of shares to buy or sell. Fractional share quantities such as 0.5 are allowed."
            }
        },
        required = new[] { "action", "ticker", "quantity" },
        additionalProperties = false
    };


    public async Task<object> ExecuteAsync(
        Guid userId,
        JsonElement arguments,
        CancellationToken ct)
    {
       
        if (!arguments.TryGetProperty("action", out var actionElement))
        {
            return new { error = "action is required" };
        }

        var actionText = actionElement.GetString()?.Trim().ToUpperInvariant();

        if (!Enum.TryParse<TradeType>(actionText, ignoreCase: true, out var action))
        {
            return new { error = "action must be BUY or SELL" };
        }

        if (!arguments.TryGetProperty("ticker", out var tickerElement))
        {
            return new { error = "ticker is required" };
        }

        var ticker = tickerElement.GetString()?.Trim().ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(ticker))
        {
            return new { error = "ticker is required" };
        }

        if (!arguments.TryGetProperty("quantity", out var quantityElement) ||
            !quantityElement.TryGetDecimal(out var quantity))
        {
            return new { error = "quantity is required and must be a number" };
        }

        if (quantity <= 0)
        {
            return new { error = "quantity must be greater than zero" };
        }

        try
        {
            var estimate = await _tradeEstimateService.EstimateTradeAsync(
                userId,
                action,
                ticker,
                quantity,
                ct);

            return estimate;
        }
        catch (ApiException ex)
        {
            return new
            {
                error = ex.Message,
                statusCode = ex.StatusCode
            };
        }
        catch (Exception ex)
        {
            return new
            {
                error = "Failed to estimate trade.",
                detail = ex.Message
            };
        }
    }
}