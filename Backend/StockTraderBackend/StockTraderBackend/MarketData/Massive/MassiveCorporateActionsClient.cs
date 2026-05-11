using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using StockTraderBackend.MarketData.Massive.DTOs;

namespace StockTraderBackend.MarketData.Massive
{
    public sealed class MassiveCorporateActionsClient : IMassiveCorporateActionsClient
    {
        private static readonly JsonSerializerOptions MassiveJsonOptions = new()
        {
            PropertyNameCaseInsensitive = false
        };

        private readonly HttpClient _http;
        private readonly MassiveOptions _options;

        public MassiveCorporateActionsClient(
            HttpClient http,
            IOptions<MassiveOptions> options)
        {
            _http = http;
            _options = options.Value;
        }

        public async Task<IReadOnlyList<MassiveSplitDto>> GetSplitsAsync(
            DateOnly fromDate,
            DateOnly toDate,
            CancellationToken ct = default)
        {
            var allResults = new List<MassiveSplitDto>();

            var nextUrl =
                $"/v3/reference/splits?" +
                $"execution_date.gte={fromDate:yyyy-MM-dd}&" +
                $"execution_date.lte={toDate:yyyy-MM-dd}&" +
                $"limit=1000&" +
                $"apiKey={_options.ApiKey}";

            while (!string.IsNullOrWhiteSpace(nextUrl))
            {
                using var response = await _http.GetAsync(nextUrl, ct);
                response.EnsureSuccessStatusCode();

                await using var stream = await response.Content.ReadAsStreamAsync(ct);
                var payload = await JsonSerializer.DeserializeAsync<MassiveSplitsResponse>(
                    stream,
                    MassiveJsonOptions,
                    ct);

                if (payload?.Results is { Count: > 0 })
                {
                    allResults.AddRange(payload.Results);
                }

                nextUrl = payload?.NextUrl;

                if (!string.IsNullOrWhiteSpace(nextUrl) &&
                    !nextUrl.Contains("apiKey=", StringComparison.OrdinalIgnoreCase))
                {
                    nextUrl += (nextUrl.Contains('?') ? "&" : "?") + $"apiKey={_options.ApiKey}";
                }
            }

            return allResults;
        }
    }
}