namespace StockTraderBackend.Trades.DTOs;

public class TradeRequest
{
    public Guid portfolioId { get; set; }
    public string ticker { get; set; } = null!;
    public decimal quantity { get; set; }
    // price comes from massive
    public TradeType tradeType { get; set; }

    public TradeRequest(Guid portfolioId, string ticker, decimal quantity, TradeType tradeType)
    {
        this.portfolioId = portfolioId;
        this.ticker = ticker;
        this.quantity = quantity;
        this.tradeType = tradeType;
    }
}