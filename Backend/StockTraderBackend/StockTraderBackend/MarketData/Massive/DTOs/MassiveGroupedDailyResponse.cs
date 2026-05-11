using System.Text.Json.Serialization;

namespace StockTraderBackend.MarketData.Massive.DTOs
{
    public sealed class MassiveGroupedDailyResponse
    {
        [JsonPropertyName("results")]
        public List<MassiveGroupedDailyBar>? Results { get; set; }

        [JsonPropertyName("resultsCount")]
        public int ResultsCount { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("request_id")]
        public string? RequestId { get; set; }
    }

}
