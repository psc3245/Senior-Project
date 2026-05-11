using StockTraderBackend.Holdings;
using StockTraderBackend.StockAPI;
using StockTraderBackend.Trades;
using StockTraderBackend.Users;

namespace StockTraderBackend.Portfolios.DTOs;

public class PortfolioDTO
{
    public Guid portfolioId { get; set; }
    public string name { get; set; }
    public decimal cashBalance { get; set; }
    public List<Trade> trades { get; set; }
    public List<Holding> holdings { get; set; }
    public decimal holdingValue { get; set; }

    public static async Task<PortfolioDTO> mapToPortfolioDTO(Portfolio portfolio, IStockAPIService stockApiService)
    {
        decimal holdingValue = 0;
        foreach (var h in portfolio.holdings)
        {
            decimal? price = await stockApiService.getPriceByTickerAsync(h.ticker);
            holdingValue += h.quantity * (price ?? h.avgBuyPrice);
        }

        return new PortfolioDTO
        {
            portfolioId = portfolio.portfolioId,
            name = portfolio.name,
            cashBalance = Math.Round(portfolio.cashBalance, 2),
            trades = portfolio.trades.ToList(),
            holdings = portfolio.holdings.ToList(),
            holdingValue = Math.Round(holdingValue, 2)
        };
    }

}