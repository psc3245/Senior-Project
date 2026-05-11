namespace StockTraderBackend.MarketData.Massive
{
    using System.Net.Http.Json;
    using System.Text.Json;
    using Microsoft.Extensions.Options;
    using StockTraderBackend.MarketData.Massive.DTOs;

    public sealed class MassiveMarketDataClient : IMassiveMarketDataClient
    {
        private static readonly JsonSerializerOptions MassiveJsonOptions = new()
        {
            PropertyNameCaseInsensitive = false
        };

        private readonly HttpClient _http;
        private readonly MassiveOptions _options;

        public MassiveMarketDataClient(
            HttpClient http,
            IOptions<MassiveOptions> options)
        {
            _http = http;
            _options = options.Value;
        }

        public async Task<MassiveGroupedDailyResponse?> GetGroupedDailyAsync(
            DateOnly date,
            CancellationToken ct = default)
        {
            var url =
                $"/v2/aggs/grouped/locale/us/market/stocks/{date:yyyy-MM-dd}" +
                $"?adjusted=true&apiKey={Uri.EscapeDataString(_options.ApiKey)}";

            using var response = await _http.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<MassiveGroupedDailyResponse>(
                MassiveJsonOptions,
                ct);
        }

        public async Task<MassiveTickerRangeResponse?> GetDailyBarsAsync(
            string ticker,
            DateOnly from,
            DateOnly to,
            CancellationToken ct = default)
        {
            var normalizedTicker = ticker.Trim().ToUpperInvariant();

            var url =
                $"/v2/aggs/ticker/{Uri.EscapeDataString(normalizedTicker)}/range/1/day/{from:yyyy-MM-dd}/{to:yyyy-MM-dd}" +
                $"?adjusted=true&sort=asc&limit=50000&apiKey={Uri.EscapeDataString(_options.ApiKey)}";

            using var response = await _http.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<MassiveTickerRangeResponse>(
                MassiveJsonOptions,
                ct);
        }
    }
}