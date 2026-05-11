using Microsoft.EntityFrameworkCore;
using StockTraderBackend.Data;
using StockTraderBackend.News.Models;

namespace StockTraderBackend.News.Repositories;

public class ArticleRepository : IArticleRepository
{
    private readonly AppDbContext _db;

    public ArticleRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Article?> GetByIdAsync(Guid articleId)
    {
        return await _db.Articles.FindAsync(articleId);
    }

    public async Task<List<Article>> SearchTodaysCachedArticlesAsync(
        string query,
        int page,
        int max)
    {
        var normalizedQuery = query.Trim().ToLower();
        var today = DateTime.UtcNow.Date;

        return await _db.Articles
            .Where(a =>
                a.CreatedAt.Date == today &&
                (
                    (!string.IsNullOrEmpty(a.Title) && a.Title.ToLower().Contains(normalizedQuery)) ||
                    (!string.IsNullOrEmpty(a.Description) && a.Description.ToLower().Contains(normalizedQuery)) ||
                    (!string.IsNullOrEmpty(a.Content) && a.Content.ToLower().Contains(normalizedQuery)) ||
                    (!string.IsNullOrEmpty(a.SourceName) && a.SourceName.ToLower().Contains(normalizedQuery))
                ))
            .OrderByDescending(a => a.PublishedAt)
            .Skip((page - 1) * max)
            .Take(max)
            .ToListAsync();
    }

    public async Task<List<Article>> GetTodaysCachedHeadlinesAsync(
        string category,
        string lang,
        string country,
        int page,
        int max)
    {
        var normalizedCategory = category.Trim().ToLower();
        var normalizedLang = lang.Trim().ToLower();
        var normalizedCountry = country.Trim().ToLower();
        var today = DateTime.UtcNow.Date;

        return await _db.Articles
            .Where(a =>
                a.CreatedAt.Date == today &&
                !string.IsNullOrEmpty(a.Category) &&
                !string.IsNullOrEmpty(a.Language) &&
                !string.IsNullOrEmpty(a.Country) &&
                a.Category.ToLower() == normalizedCategory &&
                a.Language.ToLower() == normalizedLang &&
                a.Country.ToLower() == normalizedCountry)
            .OrderByDescending(a => a.PublishedAt)
            .Skip((page - 1) * max)
            .Take(max)
            .ToListAsync();
    }

    public async Task<Dictionary<string, Article>> GetByUrlsAsync(IEnumerable<string> urls)
    {
        var urlList = urls
            .Where(u => !string.IsNullOrWhiteSpace(u))
            .Distinct()
            .ToList();

        if (urlList.Count == 0)
            return new Dictionary<string, Article>();

        var existingArticles = await _db.Articles
            .Where(a => urlList.Contains(a.Url))
            .ToListAsync();

        return existingArticles.ToDictionary(a => a.Url, a => a);
    }

    public async Task AddRangeAsync(IEnumerable<Article> articles)
    {
        var articleList = articles.ToList();

        if (articleList.Count == 0)
            return;

        _db.Articles.AddRange(articleList);
        await _db.SaveChangesAsync();
    }

    public async Task<HashSet<Guid>> GetFavoritedArticleIdsAsync(
        Guid userId,
        IEnumerable<Guid> articleIds)
    {
        var articleIdList = articleIds.Distinct().ToList();

        if (articleIdList.Count == 0)
            return new HashSet<Guid>();

        var favorites = await _db.FavoriteArticles
            .Where(f => f.UserId == userId && articleIdList.Contains(f.ArticleId))
            .Select(f => f.ArticleId)
            .ToListAsync();

        return favorites.ToHashSet();
    }
}