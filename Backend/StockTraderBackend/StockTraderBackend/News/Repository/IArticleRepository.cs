using StockTraderBackend.News.Models;

namespace StockTraderBackend.News.Repositories;

public interface IArticleRepository
{
    Task<Article?> GetByIdAsync(Guid articleId);

    Task<List<Article>> SearchTodaysCachedArticlesAsync(
        string query,
        int page,
        int max);

    Task<List<Article>> GetTodaysCachedHeadlinesAsync(
        string category,
        string lang,
        string country,
        int page,
        int max);

    Task<Dictionary<string, Article>> GetByUrlsAsync(IEnumerable<string> urls);

    Task AddRangeAsync(IEnumerable<Article> articles);

    Task<HashSet<Guid>> GetFavoritedArticleIdsAsync(
        Guid userId,
        IEnumerable<Guid> articleIds);
}