namespace StockTraderBackend.News.Models;

public class Article
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
    public string? Category { get; set; }
    public string? Language { get; set; }
    public string? Country { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

}