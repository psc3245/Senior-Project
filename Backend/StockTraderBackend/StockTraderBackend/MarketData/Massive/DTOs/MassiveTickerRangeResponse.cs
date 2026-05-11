namespace StockTraderBackend.MarketData.Massive.DTOs
{
    using System.Text.Json.Serialization;

    public sealed class MassiveTickerRangeResponse
    {
        [JsonPropertyName("ticker")]
        public string? Ticker { get; set; }

        [JsonPropertyName("queryCount")]
        public int QueryCount { get; set; }

        [JsonPropertyName("resultsCount")]
        public int ResultsCount { get; set; }

        [JsonPropertyName("adjusted")]
        public bool Adjusted { get; set; }

        [JsonPropertyName("results")]
        public List<MassiveTickerRangeBarDto>? Results { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("request_id")]
        public string? RequestId { get; set; }

        [JsonPropertyName("next_url")]
        public string? NextUrl { get; set; }
    }
}