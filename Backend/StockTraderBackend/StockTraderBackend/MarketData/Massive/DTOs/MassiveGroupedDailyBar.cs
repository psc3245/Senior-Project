using System.Text.Json.Serialization;

namespace StockTraderBackend.MarketData.Massive.DTOs
{
    public sealed class MassiveGroupedDailyBar
    {
        [JsonPropertyName("T")]
        public string Ticker { get; set; } = string.Empty;

        [JsonPropertyName("o")]
        public decimal Open { get; set; }

        [JsonPropertyName("h")]
        public decimal High { get; set; }

        [JsonPropertyName("l")]
        public decimal Low { get; set; }

        [JsonPropertyName("c")]
        public decimal Close { get; set; }

        [JsonPropertyName("v")]
        public decimal Volume { get; set; }

        [JsonPropertyName("vw")]
        public decimal? Vwap { get; set; }

        [JsonPropertyName("t")]
        public long TimestampUnixMs { get; set; }
    }
}