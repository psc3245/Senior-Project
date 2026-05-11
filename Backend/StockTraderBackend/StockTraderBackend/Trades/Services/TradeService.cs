using Microsoft.Extensions.Logging.Abstractions;
using StockTraderBackend.Holdings;
using StockTraderBackend.Portfolios;
using StockTraderBackend.Portfolios.DTOs;
using StockTraderBackend.StockAPI;
using StockTraderBackend.Trades.DTOs;
using StockTraderBackend.Trades.Repositories;
using StockTraderBackend.Users;

namespace StockTraderBackend.Trades.Services;

public class TradeService(TradeRepository tradeRepository, IPortfolioService portfolioService, IStockAPIService stockAPIService) : ITradeService
{

    // One entry point for all trade requests, just a wrapper to split into more specific behavior
    public async Task<PortfolioDTO> handleTrade(Guid portfolioId, TradeRequest tradeRequest)
    {
        var ticker = tradeRequest.ticker?.Trim().ToUpperInvariant();
        if (tradeRequest == null)
            throw new ArgumentNullException();
        if (string.IsNullOrWhiteSpace(ticker))
            throw new ArgumentException("Ticker is required.", nameof(tradeRequest));
        if (tradeRequest.quantity <= 0)
            throw new ArgumentException("Quantity must be > 0.", nameof(tradeRequest));
        if (tradeRequest.tradeType == TradeType.BUY)
            return await handleBuy(portfolioId, tradeRequest);
        if (tradeRequest.tradeType == TradeType.SELL)
            return await handleSell(portfolioId, tradeRequest);
        // if we get here its a problem. idk how to handle so we will figure out later
        throw new ArgumentNullException();
    }

    public async Task<PortfolioDTO> handleBuy(Guid portfolioId, TradeRequest tradeRequest, CancellationToken ct = default)
    {
        var ticker = tradeRequest.ticker?.Trim().ToUpperInvariant();

        decimal price = await stockAPIService.getPriceByTickerAsync(ticker, ct)
            ?? throw new ArgumentException("Price cannot be null. Make sure your ticker is correct.");

        decimal priceOfTrade = tradeRequest.quantity * price;

        var portfolio = await portfolioService.getPortfolioEntityById(portfolioId, ct);
        if (portfolio == null) throw new KeyNotFoundException($"Portfolio {portfolioId} not found.");

        if (portfolio.cashBalance < priceOfTrade)
            throw new InvalidOperationException("Insufficient funds.");

        portfolio.cashBalance -= priceOfTrade;

        var existing = portfolio.holdings.FirstOrDefault(h => h.ticker == ticker);
        if (existing != null)
        {
            existing.avgBuyPrice =
                (existing.quantity * existing.avgBuyPrice + tradeRequest.quantity * price)
                / (existing.quantity + tradeRequest.quantity);
            existing.quantity += tradeRequest.quantity;
        }
        else
        {
            portfolio.holdings.Add(new Holding(portfolioId, ticker, tradeRequest.quantity, price));
        }

        Trade trade = new Trade(portfolioId, ticker, tradeRequest.tradeType, tradeRequest.quantity, price, priceOfTrade);
        await tradeRepository.addTrade(trade);

        await portfolioService.updatePortfolio(portfolio, ct);

        return await PortfolioDTO.mapToPortfolioDTO(portfolio, stockAPIService);
    }

    public async Task<PortfolioDTO> handleSell(Guid portfolioId, TradeRequest tradeRequest, CancellationToken ct = default)
    {
        var ticker = tradeRequest.ticker?.Trim().ToUpperInvariant();

        decimal price = await stockAPIService.getPriceByTickerAsync(ticker, ct)
            ?? throw new ArgumentException("Price cannot be null. Make sure your ticker is correct.");

        var portfolio = await portfolioService.getPortfolioEntityById(portfolioId, ct);
        if (portfolio == null) throw new KeyNotFoundException($"Portfolio {portfolioId} not found.");

        var existing = portfolio.holdings.FirstOrDefault(h => h.ticker == ticker);
        if (existing == null)
            throw new InvalidOperationException($"Stock {ticker} not held by portfolio {portfolioId}.");
        if (existing.quantity < tradeRequest.quantity)
            throw new InvalidOperationException($"Insufficient shares to sell {tradeRequest.quantity} of {ticker}.");

        if (existing.quantity == tradeRequest.quantity)
            portfolio.holdings.Remove(existing);
        else
            existing.quantity -= tradeRequest.quantity;

        portfolio.cashBalance += tradeRequest.quantity * price;

        Trade trade = new Trade(portfolioId, ticker, tradeRequest.tradeType, tradeRequest.quantity, price, tradeRequest.quantity * price);
        await tradeRepository.addTrade(trade);

        await portfolioService.updatePortfolio(portfolio, ct);

        return await PortfolioDTO.mapToPortfolioDTO(portfolio, stockAPIService);
    }

    public async void saveTrade(Guid portfolioId, TradeRequest tradeRequest, decimal sharePrice)
    {
        Trade trade = new Trade(
            portfolioId, tradeRequest.ticker, tradeRequest.tradeType,
            tradeRequest.quantity, sharePrice, tradeRequest.quantity * sharePrice
        );
        await tradeRepository.addTrade(trade);
    }
}