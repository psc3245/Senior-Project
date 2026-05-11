using System.Text.Json.Serialization;

namespace StockTraderBackend.Holdings;
using StockTraderBackend.Portfolios;

public class Holding
{
    public Guid holdingId { get; set; }
    public Guid portfolioId { get; set; }
    [JsonIgnore]
    public Portfolio portfolio { get; set; } = null!;
    public string ticker { get; set; } = null!;
    public decimal quantity { get; set; }
    public decimal avgBuyPrice { get; set; }

    public Holding(Guid portfolioId, string ticker, decimal quantity, decimal avgBuyPrice)
    {
        this.holdingId = Guid.NewGuid();
        this.portfolioId = portfolioId;
        this.ticker = ticker;
        this.quantity = quantity;
        this.avgBuyPrice = avgBuyPrice;
    }
}