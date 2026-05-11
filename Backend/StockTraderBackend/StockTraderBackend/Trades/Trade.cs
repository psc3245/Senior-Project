using System.Text.Json.Serialization;

namespace StockTraderBackend.Trades;

using StockTraderBackend.Portfolios;

public class Trade
{

    public Guid tradeId { get; set; }
    public Guid portfolioId { get; set; }
    [JsonIgnore]
    public Portfolio portfolio { get; set; } = null!;

    public string ticker { get; set; } = null!;
    public TradeType tradeType { get; set; }

    public decimal quantity { get; set; }
    public decimal priceAtTrade { get; set; }
    public decimal totalValue { get; set; }
    public DateTime executedAt { get; set; } = DateTime.UtcNow;

    public Trade(Guid portfolioId, string ticker, TradeType tradeType, decimal quantity, decimal priceAtTrade,
        decimal totalValue)
    {
        this.tradeId = Guid.NewGuid();
        this.portfolioId = portfolioId;
        this.ticker = ticker;
        this.tradeType = tradeType;
        this.quantity = quantity;
        this.priceAtTrade = priceAtTrade;
        this.totalValue = totalValue;
        this.executedAt = DateTime.UtcNow;
    }
    
}