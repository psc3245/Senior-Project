using StockTraderBackend.News.DTOs;

namespace StockTraderBackend.News.Services
{
    public interface IFavoriteArticleService
    {
        Task<bool> FavoriteArticleAsync(Guid userId, Guid articleId);
        Task<List<ArticleResponseDto>> GetFavoritesAsync(Guid userId);
        Task<bool> RemoveFavoriteAsync(Guid userId, Guid articleId);
    }
}