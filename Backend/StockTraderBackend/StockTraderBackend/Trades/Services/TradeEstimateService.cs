using StockTraderBackend.Middleware;
using StockTraderBackend.Portfolios;
using StockTraderBackend.StockAPI;
using StockTraderBackend.Trades.DTOs;

namespace StockTraderBackend.Trades.Services
{
    public sealed class TradeEstimateService : ITradeEstimateService
    {
        private readonly IPortfolioService _portfolioService;
        private readonly IStockAPIService _stockApiService;

        public TradeEstimateService(
            IPortfolioService portfolioService,
            IStockAPIService stockApiService)
        {
            _portfolioService = portfolioService;
            _stockApiService = stockApiService;
        }

        public async Task<TradeEstimateDto> EstimateTradeAsync(
    Guid userId,
    TradeType action,
    string ticker,
    decimal quantity,
    CancellationToken ct = default)
        {
            ticker = ticker.Trim().ToUpperInvariant();

            if (string.IsNullOrWhiteSpace(ticker))
                throw new ApiException(400, "Ticker is required.");

            if (quantity <= 0)
                throw new ApiException(400, "Quantity must be greater than zero.");

            var portfolio = await _portfolioService.getPortfolioEntityByUserId(userId, ct);

            if (portfolio == null)
                throw new ApiException(404, $"No portfolio found for user '{userId}'.");

            var estimatedPrice = await _stockApiService.getPriceByTickerAsync(ticker, ct);

            if (estimatedPrice == null)
                throw new ApiException(404, $"No price found for ticker '{ticker}'.");

            var estimatedTradeValue = quantity * estimatedPrice.Value;

            var existingHolding = portfolio.holdings
                .FirstOrDefault(h => h.ticker.ToUpperInvariant() == ticker);

            var currentSharesOwned = existingHolding?.quantity ?? 0m;

            var estimatedCashAfterTrade = action == TradeType.BUY
                ? portfolio.cashBalance - estimatedTradeValue
                : portfolio.cashBalance + estimatedTradeValue;

            var canExecute = true;
            string? blockingReason = null;

            if (action == TradeType.BUY && portfolio.cashBalance < estimatedTradeValue)
            {
                canExecute = false;
                blockingReason = "Not enough cash to complete this buy order.";
            }

            if (action == TradeType.SELL && currentSharesOwned < quantity)
            {
                canExecute = false;
                blockingReason = "Not enough shares to complete this sell order.";
            }

            return new TradeEstimateDto
            {
                PortfolioId = portfolio.portfolioId,
                Action = action,
                Ticker = ticker,
                Quantity = quantity,

                EstimatedPrice = Math.Round(estimatedPrice.Value, 2),
                EstimatedTradeValue = Math.Round(estimatedTradeValue, 2),

                CashBalance = Math.Round(portfolio.cashBalance, 2),
                EstimatedCashAfterTrade = Math.Round(estimatedCashAfterTrade, 2),

                CurrentSharesOwned = currentSharesOwned,

                CanExecute = canExecute,
                BlockingReason = blockingReason,

                RequiredConfirmation = TradeConfirmationHelper.BuildRequiredConfirmation(
                    action,
                    quantity,
                    ticker)
            };
        }
    }
}
