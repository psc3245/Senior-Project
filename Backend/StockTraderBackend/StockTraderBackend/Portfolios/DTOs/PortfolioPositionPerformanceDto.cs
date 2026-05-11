namespace StockTraderBackend.Portfolios.DTOs
{
    public sealed class PortfolioPositionPerformanceDto
    {
        public string Ticker { get; set; } = string.Empty;

        public decimal Quantity { get; set; }
        public decimal AverageBuyPrice { get; set; }
        public decimal CurrentPrice { get; set; }

        public decimal MarketValue { get; set; }
        public decimal CostBasis { get; set; }
        public decimal GainLoss { get; set; }
        public decimal GainLossPercent { get; set; }
    }
}
