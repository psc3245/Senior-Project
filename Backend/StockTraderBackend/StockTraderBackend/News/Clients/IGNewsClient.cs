using StockTraderBackend.News.Models;

namespace StockTraderBackend.News.Clients;

public interface IGNewsClient
{
    Task<List<GNewsArticle>> SearchArticlesAsync(
        string q,
        string lang,
        string country,
        int page,
        int max);

    Task<List<GNewsArticle>> GetHeadlinesAsync(
        string category,
        string lang,
        string country,
        int page,
        int max);
}