using System.Text.Json.Serialization;

namespace StockTraderBackend.Trades.DTOs
{
    public sealed class TradeEstimateDto
    {
        public Guid PortfolioId { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public TradeType Action { get; set; }
        public string Ticker { get; set; } = string.Empty;
        public decimal Quantity { get; set; }

        public decimal EstimatedPrice { get; set; }
        public decimal EstimatedTradeValue { get; set; }

        public decimal CashBalance { get; set; }
        public decimal EstimatedCashAfterTrade { get; set; }

        public decimal CurrentSharesOwned { get; set; }

        public bool CanExecute { get; set; }
        public string? BlockingReason { get; set; }

        public string RequiredConfirmation { get; set; } = string.Empty;

        public string Note { get; set; } =
            "This is an estimate using the latest available price. The final execution price may differ.";
    }
}
