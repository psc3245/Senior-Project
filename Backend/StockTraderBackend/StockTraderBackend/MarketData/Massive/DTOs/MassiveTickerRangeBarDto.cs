namespace StockTraderBackend.MarketData.Massive.DTOs
{
    using System.Text.Json.Serialization;

    public sealed class MassiveTickerRangeBarDto
    {
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

        [JsonPropertyName("t")]
        public long TimestampUnixMs { get; set; }
    }
}