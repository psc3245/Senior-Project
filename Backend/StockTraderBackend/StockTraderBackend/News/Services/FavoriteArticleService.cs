using Microsoft.EntityFrameworkCore;
using StockTraderBackend.Data;
using StockTraderBackend.News.DTOs;
using StockTraderBackend.News.Models;

namespace StockTraderBackend.News.Services;

public class FavoriteArticleService : IFavoriteArticleService
{
    private readonly AppDbContext _db;

    public FavoriteArticleService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<bool> FavoriteArticleAsync(Guid userId, Guid articleId)
    {
        var articleExists = await _db.Articles.AnyAsync(a => a.Id == articleId);
        if (!articleExists)
            return false;

        var alreadyFavorited = await _db.FavoriteArticles
            .AnyAsync(f => f.UserId == userId && f.ArticleId == articleId);

        if (alreadyFavorited)
            return false;

        var favorite = new FavoriteArticle
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ArticleId = articleId,
            FavoritedAt = DateTime.UtcNow
        };

        _db.FavoriteArticles.Add(favorite);
        await _db.SaveChangesAsync();

        return true;
    }

    public async Task<List<ArticleResponseDto>> GetFavoritesAsync(Guid userId)
    {
        var favorites = await _db.FavoriteArticles
            .Where(f => f.UserId == userId)
            .OrderByDescending(f => f.FavoritedAt)
            .Join(
                _db.Articles,
                f => f.ArticleId,
                a => a.Id,
                (f, a) => new ArticleResponseDto
                {
                    Id = a.Id,
                    Title = a.Title,
                    Description = a.Description,
                    Content = a.Content,
                    Url = a.Url,
                    Image = a.Image,
                    PublishedAt = a.PublishedAt,
                    SourceName = a.SourceName,
                    SourceUrl = a.SourceUrl,
                    Favorited = true
                })
            .ToListAsync();

        return favorites;
    }

    public async Task<bool> RemoveFavoriteAsync(Guid userId, Guid articleId)
    {
        var favorite = await _db.FavoriteArticles
            .FirstOrDefaultAsync(f => f.UserId == userId && f.ArticleId == articleId);

        if (favorite == null)
            return false;

        _db.FavoriteArticles.Remove(favorite);
        await _db.SaveChangesAsync();

        return true;
    }
}