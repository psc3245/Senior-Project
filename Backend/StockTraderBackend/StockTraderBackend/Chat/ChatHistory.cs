namespace StockTraderBackend.Chat;

public class ChatHistory
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string SessionName { get; set; } = null!;
    public List<ChatMessage> Messages { get; set; } = new();

    public ChatHistory(Guid userId, string sessionName)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        SessionName = sessionName;
    }
}