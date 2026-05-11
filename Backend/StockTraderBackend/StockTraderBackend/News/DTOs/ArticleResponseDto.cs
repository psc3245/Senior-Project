namespace StockTraderBackend.News.DTOs
{
    public class ArticleResponseDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Content { get; set; }
        public string Url { get; set; }
        public string Image { get; set; }
        public DateTime PublishedAt { get; set; }
        public string SourceName { get; set; }
        public string SourceUrl { get; set; }
        public bool Favorited { get; set; }
    }
}
