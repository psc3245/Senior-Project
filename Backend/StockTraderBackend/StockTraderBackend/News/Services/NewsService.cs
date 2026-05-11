using StockTraderBackend.News.Clients;
using StockTraderBackend.News.DTOs;
using StockTraderBackend.News.Models;
using StockTraderBackend.News.Repositories;

namespace StockTraderBackend.News.Services;

public class NewsService : INewsService
{
    private readonly IArticleRepository _articleRepository;
    private readonly IGNewsClient _gnewsClient;

    public NewsService(
        IArticleRepository articleRepository,
        IGNewsClient gnewsClient)
    {
        _articleRepository = articleRepository;
        _gnewsClient = gnewsClient;
    }

    public async Task<Article> GetArticleByIdAsync(Guid articleId)
    {
        var article = await _articleRepository.GetByIdAsync(articleId);

        if (article == null)
            throw new Exception("Article not found");

        return article;
    }

    public async Task<List<ArticleResponseDto>> SearchArticlesAsync(
        string q,
        string lang,
        string country,
        int page,
        int max,
        Guid userId)
    {
        var cachedArticles = await _articleRepository.SearchTodaysCachedArticlesAsync(
            q,
            page,
            max);

        if (cachedArticles.Count > 0)
            return await MapArticlesToDtosAsync(cachedArticles, userId);

        var gnewsArticles = await _gnewsClient.SearchArticlesAsync(
            q,
            lang,
            country,
            page,
            max);

        if (gnewsArticles.Count == 0)
            return new List<ArticleResponseDto>();

        return await SaveAndMapArticlesAsync(
            gnewsArticles,
            userId,
            category: null,
            lang: lang,
            country: country);
    }

    public async Task<List<ArticleResponseDto>> GetHeadlinesAsync(
        string category,
        string lang,
        string country,
        int page,
        int max,
        Guid userId)
    {
        var cachedArticles = await _articleRepository.GetTodaysCachedHeadlinesAsync(
            category,
            lang,
            country,
            page,
            max);

        if (cachedArticles.Count > 0)
            return await MapArticlesToDtosAsync(cachedArticles, userId);

        var gnewsArticles = await _gnewsClient.GetHeadlinesAsync(
            category,
            lang,
            country,
            page,
            max);

        if (gnewsArticles.Count == 0)
            return new List<ArticleResponseDto>();

        return await SaveAndMapArticlesAsync(
            gnewsArticles,
            userId,
            category: category,
            lang: lang,
            country: country);
    }

    private async Task<List<ArticleResponseDto>> MapArticlesToDtosAsync(
        List<Article> articles,
        Guid userId)
    {
        var articleIds = articles.Select(a => a.Id).ToList();

        var favoriteSet = await _articleRepository.GetFavoritedArticleIdsAsync(
            userId,
            articleIds);

        return articles
            .Select(article => new ArticleResponseDto
            {
                Id = article.Id,
                Title = article.Title,
                Description = article.Description,
                Content = article.Content,
                Url = article.Url,
                Image = article.Image,
                PublishedAt = article.PublishedAt,
                SourceName = article.SourceName,
                SourceUrl = article.SourceUrl,
                Favorited = favoriteSet.Contains(article.Id)
            })
            .ToList();
    }

    private async Task<List<ArticleResponseDto>> SaveAndMapArticlesAsync(
        List<GNewsArticle> gnewsArticles,
        Guid userId,
        string? category,
        string? lang,
        string? country)
    {
        var urls = gnewsArticles
            .Where(a => !string.IsNullOrWhiteSpace(a.url))
            .Select(a => a.url)
            .Distinct()
            .ToList();

        var existingByUrl = await _articleRepository.GetByUrlsAsync(urls);

        var newArticles = new List<Article>();

        foreach (var gnewsArticle in gnewsArticles)
        {
            if (string.IsNullOrWhiteSpace(gnewsArticle.url))
                continue;

            if (!existingByUrl.ContainsKey(gnewsArticle.url))
            {
                var article = new Article
                {
                    Id = Guid.NewGuid(),
                    Title = gnewsArticle.title,
                    Description = gnewsArticle.description,
                    Content = gnewsArticle.content,
                    Url = gnewsArticle.url,
                    Image = gnewsArticle.image,
                    PublishedAt = gnewsArticle.publishedAt,
                    SourceName = gnewsArticle.source?.name,
                    SourceUrl = gnewsArticle.source?.url,
                    Category = category,
                    Language = lang,
                    Country = country,
                    CreatedAt = DateTime.UtcNow
                };

                newArticles.Add(article);
                existingByUrl[gnewsArticle.url] = article;
            }
        }

        await _articleRepository.AddRangeAsync(newArticles);

        var orderedArticles = gnewsArticles
            .Where(a => !string.IsNullOrWhiteSpace(a.url) && existingByUrl.ContainsKey(a.url))
            .Select(a => existingByUrl[a.url])
            .ToList();

        return await MapArticlesToDtosAsync(orderedArticles, userId);
    }
}