namespace StockTraderBackend.News.Models
{
    public class GNewsResponse
    {
        public int totalArticles { get; set; }
        public List<GNewsArticle> articles { get; set; } = new();
    }
}
