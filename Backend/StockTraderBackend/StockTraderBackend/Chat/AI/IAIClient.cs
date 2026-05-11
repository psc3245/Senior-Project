namespace StockTraderBackend.Chat.AI;

public interface IAIClient
{
    Task<string> SendMessageAsync(Guid userId, List<ChatMessage> history, string newMessage);
}