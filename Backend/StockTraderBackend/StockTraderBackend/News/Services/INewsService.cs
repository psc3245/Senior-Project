using StockTraderBackend.News.DTOs;
using StockTraderBackend.News.Models;

namespace StockTraderBackend.News.Services
{
    public interface INewsService
    {
        Task<Article> GetArticleByIdAsync(Guid articleId);
        Task<List<ArticleResponseDto>> GetHeadlinesAsync(string category, string lang, string country, int page, int max, Guid userId);
        Task<List<ArticleResponseDto>> SearchArticlesAsync(string q, string lang, string country, int page, int max, Guid userId);
    }
}