using System.Text.Json;
using StockTraderBackend.Chat.LLMTools;
using StockTraderBackend.Middleware;
using StockTraderBackend.Portfolios;
using StockTraderBackend.Trades;
using StockTraderBackend.Trades.DTOs;
using StockTraderBackend.Trades.Services;

namespace StockTraderBackend.Chat.Tools.Level_3;

public sealed class ExecuteTradeTool : ILlmTool
{
    private readonly IPortfolioService _portfolioService;
    private readonly ITradeService _tradeService;

    public ExecuteTradeTool(
        IPortfolioService portfolioService,
        ITradeService tradeService)
    {
        _portfolioService = portfolioService;
        _tradeService = tradeService;
    }

    public string Name => "execute_trade";

    public string Description =>
        "Executes a buy or sell trade using the current user's default portfolio. This tool modifies the portfolio. Only use this tool after the user explicitly confirms the exact action, quantity, and ticker using the required confirmation phrase from estimate_trade, such as 'confirm buy 5 AAPL' or 'confirm sell 2 HD'. Never use this tool for estimates or previews. If success is false, clearly tell the user the trade failed and do not say it executed.";

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
            },
            confirmationText = new
            {
                type = "string",
                description = "The exact confirmation text from the user, such as 'confirm buy 5 AAPL'."
            }
        },
        required = new[] { "action", "ticker", "quantity", "confirmationText" },
        additionalProperties = false
    };

    public async Task<object> ExecuteAsync(
        Guid userId,
        JsonElement arguments,
        CancellationToken ct)
    {
        if (!arguments.TryGetProperty("action", out var actionElement))
        {
            return new
            {
                success = false,
                message = "Trade failed: action is required."
            };
        }

        var actionText = actionElement.GetString()?.Trim().ToUpperInvariant();

        if (!Enum.TryParse<TradeType>(actionText, ignoreCase: true, out var action))
        {
            return new
            {
                success = false,
                message = "Trade failed: action must be BUY or SELL."
            };
        }

        if (!arguments.TryGetProperty("ticker", out var tickerElement))
        {
            return new
            {
                success = false,
                message = "Trade failed: ticker is required."
            };
        }

        var ticker = tickerElement.GetString()?.Trim().ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(ticker))
        {
            return new
            {
                success = false,
                message = "Trade failed: ticker is required."
            };
        }

        if (!arguments.TryGetProperty("quantity", out var quantityElement) ||
            !quantityElement.TryGetDecimal(out var quantity))
        {
            return new
            {
                success = false,
                message = "Trade failed: quantity is required and must be a number.",
                attemptedTrade = new
                {
                    action = action.ToString(),
                    ticker
                }
            };
        }

        if (quantity <= 0)
        {
            return new
            {
                success = false,
                message = "Trade failed: quantity must be greater than zero.",
                attemptedTrade = new
                {
                    action = action.ToString(),
                    ticker,
                    quantity
                }
            };
        }

        var requiredConfirmation = TradeConfirmationHelper.BuildRequiredConfirmation(
            action,
            quantity,
            ticker);

        if (!arguments.TryGetProperty("confirmationText", out var confirmationTextElement))
        {
            return new
            {
                success = false,
                message = "Trade failed: explicit confirmation is required.",
                requiredConfirmation,
                attemptedTrade = new
                {
                    action = action.ToString(),
                    ticker,
                    quantity
                }
            };
        }

        var confirmationText = confirmationTextElement.GetString() ?? string.Empty;

        if (!TradeConfirmationHelper.Matches(
                confirmationText,
                action,
                quantity,
                ticker))
        {
            return new
            {
                success = false,
                message = "Trade failed: confirmation text did not match the required confirmation phrase.",
                requiredConfirmation,
                providedConfirmation = confirmationText,
                attemptedTrade = new
                {
                    action = action.ToString(),
                    ticker,
                    quantity
                }
            };
        }

        Console.WriteLine($"[EXECUTE_TRADE] Attempting {action} {quantity:g} {ticker} for user {userId}");

        try
        {
            var portfolio = await _portfolioService.getPortfolioEntityByUserId(
                userId,
                ct);

            if (portfolio == null)
            {
                Console.WriteLine($"[EXECUTE_TRADE] FAILED {action} {quantity:g} {ticker}: no portfolio found for user {userId}");

                return new
                {
                    success = false,
                    message = "Trade failed: no portfolio found for the current user.",
                    attemptedTrade = new
                    {
                        action = action.ToString(),
                        ticker,
                        quantity
                    }
                };
            }

            var tradeRequest = new TradeRequest(
                portfolio.portfolioId,
                ticker,
                quantity,
                action);

            var result = await _tradeService.handleTrade(
                portfolio.portfolioId,
                tradeRequest);

            Console.WriteLine($"[EXECUTE_TRADE] SUCCESS {action} {quantity:g} {ticker} for portfolio {portfolio.portfolioId}");

            return new
            {
                success = true,
                message = $"Trade executed successfully: {action} {quantity:g} {ticker}.",
                executedTrade = new
                {
                    portfolioId = portfolio.portfolioId,
                    action = action.ToString(),
                    ticker,
                    quantity
                },
                portfolio = result
            };
        }
        catch (ApiException ex)
        {
            Console.WriteLine($"[EXECUTE_TRADE] FAILED {action} {quantity:g} {ticker}: {ex.Message}");

            return new
            {
                success = false,
                message = $"Trade failed: {ex.Message}",
                statusCode = ex.StatusCode,
                attemptedTrade = new
                {
                    action = action.ToString(),
                    ticker,
                    quantity
                }
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[EXECUTE_TRADE] FAILED {action} {quantity:g} {ticker}: {ex}");

            return new
            {
                success = false,
                message = "Trade failed.",
                detail = ex.Message,
                attemptedTrade = new
                {
                    action = action.ToString(),
                    ticker,
                    quantity
                }
            };
        }
    }
}