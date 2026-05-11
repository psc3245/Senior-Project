using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using StockTraderBackend.News.Models;

namespace StockTraderBackend.News.Clients;

public class GNewsClient : IGNewsClient
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public GNewsClient(
        IConfiguration config,
        IHttpClientFactory factory)
    {
        _httpClient = factory.CreateClient();
        _apiKey = config["GNews:ApiKey"]!;
    }

    public async Task<List<GNewsArticle>> SearchArticlesAsync(
        string q,
        string lang,
        string country,
        int page,
        int max)
    {
        var url =
            $"https://gnews.io/api/v4/search?q={Uri.EscapeDataString(q)}" +
            $"&lang={lang}&country={country}&page={page}&max={max}&apikey={_apiKey}";

        return await SendRequestAsync(url);
    }

    public async Task<List<GNewsArticle>> GetHeadlinesAsync(
        string category,
        string lang,
        string country,
        int page,
        int max)
    {
        var url =
            $"https://gnews.io/api/v4/top-headlines?category={Uri.EscapeDataString(category)}" +
            $"&lang={lang}&country={country}&page={page}&max={max}&apikey={_apiKey}";

        return await SendRequestAsync(url);
    }

    private async Task<List<GNewsArticle>> SendRequestAsync(string url)
    {
        var response = await _httpClient.GetAsync(url);

        if (response.StatusCode == HttpStatusCode.Forbidden)
            return new List<GNewsArticle>();

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();

        var gnews = JsonSerializer.Deserialize<GNewsResponse>(
            json,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

        return gnews?.articles ?? new List<GNewsArticle>();
    }
}