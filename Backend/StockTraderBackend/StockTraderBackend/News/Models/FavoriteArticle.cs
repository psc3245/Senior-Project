namespace StockTraderBackend.News.Models;
public class FavoriteArticle
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid ArticleId { get; set; }
    public DateTime FavoritedAt { get; set; } = DateTime.UtcNow;
}