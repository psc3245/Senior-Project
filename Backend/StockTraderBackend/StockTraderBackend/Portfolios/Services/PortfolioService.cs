using StockTraderBackend.Holdings;
using StockTraderBackend.Portfolios.DTOs;
using StockTraderBackend.StockAPI;
using StockTraderBackend.Trades.DTOs;

namespace StockTraderBackend.Portfolios;

public class PortfolioService(PortfolioRepository portfolioRepository, IStockAPIService stockApiService) : IPortfolioService
{
    public async Task<ICollection<PortfolioDTO>> getAllPortfolios(CancellationToken ct = default)
    {
        var portfolios = await portfolioRepository.getPortfolios(ct);

        var dtos = new List<PortfolioDTO>();

        foreach (var p in portfolios)
        {
            var dto = await PortfolioDTO.mapToPortfolioDTO(p, stockApiService);
            dtos.Add(dto);
        }

        return dtos;
    }

    public async Task<bool> UserOwnsPortfolio(Guid userId, Guid portfolioId, CancellationToken ct = default)
    {
        var portfolio = await portfolioRepository.getPortfolioById(portfolioId, ct);

        if (portfolio == null)
        {
            return false;
        }

        return portfolio.userId == userId;
    }

    public async Task<PortfolioDTO> getPortfolioById(Guid id, CancellationToken ct = default)
    {
        var p = await portfolioRepository.getPortfolioById(id, ct);
        return await PortfolioDTO.mapToPortfolioDTO(p, stockApiService);
    }

    public async Task<PortfolioDTO> getPortfolioByUserId(Guid userId, CancellationToken ct = default)
    {
        var p = await portfolioRepository.getPortfolioByUserId(userId, ct);
        return await PortfolioDTO.mapToPortfolioDTO(p, stockApiService);
    }
    public async Task<Portfolio?> getPortfolioEntityByUserId(
        Guid userId,
        CancellationToken ct = default)
    {
        var p = await portfolioRepository.getPortfolioByUserId(userId, ct);
        return p;
    }

    public async Task createPortfolio(Portfolio portfolio, CancellationToken ct = default)
    {
        await portfolioRepository.addPortfolio(portfolio, ct);
    }

    public async Task deletePortfolio(Guid id, CancellationToken ct = default)
    {
        await portfolioRepository.deletePortfolioById(id, ct);
    }

    public async Task deductCash(Guid portfolioId, decimal amount, CancellationToken ct = default)
    {
        var portfolio = await portfolioRepository.getPortfolioById(portfolioId, ct);
        if (portfolio == null) throw new KeyNotFoundException($"Portfolio not found for portfolio id {portfolioId}");
        if (portfolio.cashBalance < amount) throw new InvalidOperationException(
            $"Insufficient funds in portfolio id {portfolioId} to make transaction of {amount} dollars.");

        portfolio.cashBalance -= amount;
        await portfolioRepository.updatePortfolio(portfolio, ct);
    }

    public async Task addCash(Guid portfolioId, decimal amount, CancellationToken ct = default)
    {
        var portfolio = await portfolioRepository.getPortfolioById(portfolioId, ct);
        if (portfolio == null) throw new KeyNotFoundException($"Portfolio not found for portfolio id {portfolioId}");

        portfolio.cashBalance += amount;
        await portfolioRepository.updatePortfolio(portfolio, ct);
    }

    // upsert = update if exists, insert if not. shoutout claude
    public async Task upsertHolding(Guid portfolioId, Holding holding, CancellationToken ct = default)
    {
        if (portfolioId != holding.portfolioId)
            throw new InvalidOperationException("Provided portfolio id does not match portfolio id of holding");

        var portfolio = await portfolioRepository.getPortfolioById(portfolioId, ct);
        if (portfolio == null)
            throw new KeyNotFoundException($"Portfolio not found for portfolio id {portfolioId}");

        if (portfolio.holdings.Any(h => h.ticker == holding.ticker))
        {
            // Modifies the element in place, no need to re add to list
            var existing = portfolio.holdings.First(h => h.ticker == holding.ticker);
            existing.avgBuyPrice =
                ((existing.quantity * existing.avgBuyPrice) + (holding.quantity * holding.avgBuyPrice))
                / (existing.quantity + holding.quantity);

            existing.quantity += holding.quantity;
        }
        else
        {
            portfolio.holdings.Add(holding);
        }

        await portfolioRepository.updatePortfolio(portfolio, ct);
    }

    public async Task tryHandleSell(Guid portfolioId, TradeRequest tradeRequest, decimal price, CancellationToken ct = default)
    {
        var portfolio = await portfolioRepository.getPortfolioById(portfolioId, ct);
        if (portfolio == null) throw new KeyNotFoundException($"Portfolio not found for portfolio id {portfolioId}");

        if (portfolio.holdings.Any(h => h.ticker == tradeRequest.ticker))
        {
            var existing = portfolio.holdings.First(h => h.ticker == tradeRequest.ticker);

            if (existing.quantity < tradeRequest.quantity)
                throw new InvalidOperationException(
                    $"Insufficient shares in portfolio id {portfolioId} to sell {tradeRequest.quantity} shares of {tradeRequest.ticker}");

            if (existing.quantity == tradeRequest.quantity)
                portfolio.holdings.Remove(existing);
            else
                existing.quantity -= tradeRequest.quantity;

            portfolio.cashBalance += tradeRequest.quantity * price;
        }
        else
        {
            throw new InvalidOperationException(
                $"Invalid operation - stock {tradeRequest.ticker} not held by portfolio id {portfolioId}");
        }

        await portfolioRepository.updatePortfolio(portfolio, ct);
    }

    // Returns the raw entity, not a DTO, so TradeService can mutate it
    public async Task<Portfolio?> getPortfolioEntityById(Guid id, CancellationToken ct = default)
    {
        return await portfolioRepository.getPortfolioById(id, ct);
    }

    public async Task updatePortfolio(Portfolio portfolio, CancellationToken ct = default)
    {
        await portfolioRepository.updatePortfolio(portfolio, ct);
    }
}
