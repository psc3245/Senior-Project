namespace StockTraderBackend.Chat;
public class ChatMessage
{
    public Guid Id { get; set; }
    public Guid ChatHistoryId { get; set; }
    public string Role { get; set; } = null!; // "user" or "assistant"
    public string Message { get; set; } = null!;
    public DateTime Timestamp { get; set; }
}