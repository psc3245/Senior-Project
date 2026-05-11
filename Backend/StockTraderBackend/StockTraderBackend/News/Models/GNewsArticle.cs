namespace StockTraderBackend.News.Models
{
    public class GNewsArticle
    {
        public string title { get; set; }
        public string description { get; set; }
        public string content { get; set; }
        public string url { get; set; }
        public string image { get; set; }
        public DateTime publishedAt { get; set; }
        public GNewsSource source { get; set; }
    }
}
