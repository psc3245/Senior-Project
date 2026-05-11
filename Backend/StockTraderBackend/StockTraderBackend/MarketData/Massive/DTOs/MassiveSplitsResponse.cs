using System.Text.Json.Serialization;

namespace StockTraderBackend.MarketData.Massive.DTOs
{
    public sealed class MassiveSplitsResponse
    {
        [JsonPropertyName("results")]
        public List<MassiveSplitDto>? Results { get; set; }

        [JsonPropertyName("next_url")]
        public string? NextUrl { get; set; }
    }

    public sealed class MassiveSplitDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("ticker")]
        public string Ticker { get; set; } = string.Empty;

        [JsonPropertyName("execution_date")]
        public DateOnly ExecutionDate { get; set; }

        [JsonPropertyName("split_from")]
        public decimal SplitFrom { get; set; }

        [JsonPropertyName("split_to")]
        public decimal SplitTo { get; set; }
    }
}
