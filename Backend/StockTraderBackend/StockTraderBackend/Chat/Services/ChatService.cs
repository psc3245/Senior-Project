using StockTraderBackend.Chat.AI;

namespace StockTraderBackend.Chat;

public class ChatService
{
    private readonly ChatRepository _chatRepository;
    private readonly IAIClient _aiClient;
    private readonly ILogger<ChatService> _logger;

    public ChatService(ChatRepository chatRepository, IAIClient aiClient, ILogger<ChatService> logger)
    {
        _chatRepository = chatRepository;
        _aiClient = aiClient;
        _logger = logger;
    }

    public async Task<ChatHistory> CreateChatHistory(Guid userId, string sessionName)
    {
        return await _chatRepository.CreateChatHistory(userId, sessionName);
    }

    public async Task<ChatHistory?> GetChatHistoryById(Guid userId, Guid chatId)
    {
        var history = await _chatRepository.GetChatHistoryById(chatId);
        if (history == null) return null;
        if (history.UserId != userId) throw new UnauthorizedAccessException("You do not own this chat history.");
        return history;
    }

    public async Task<List<ChatHistory>> GetAllChatHistoriesForUser(Guid userId)
    {
        return await _chatRepository.GetAllChatHistoriesForUser(userId);
    }

    public async Task<ChatHistory?> SendMessage(Guid userId, Guid chatId, string message)
    {
        var history = await GetChatHistoryById(userId, chatId);
        if (history == null) return null;

        var userMessage = new ChatMessage
        {
            Id = Guid.NewGuid(),
            ChatHistoryId = chatId,
            Role = "user",
            Message = message,
            Timestamp = DateTime.UtcNow
        };

        await _chatRepository.AddMessageToHistory(userMessage, chatId);

        var aiResponse = await _aiClient.SendMessageAsync(userId, history.Messages, message);

        var assistantMessage = new ChatMessage
        {
            Id = Guid.NewGuid(),
            ChatHistoryId = chatId,
            Role = "assistant",
            Message = aiResponse,
            Timestamp = DateTime.UtcNow
        };

        return await _chatRepository.AddMessageToHistory(assistantMessage, chatId);
    }

    public async Task UpdateSessionName(Guid userId, Guid chatId, string newName)
    {
        var history = await GetChatHistoryById(userId, chatId);
        if (history == null) return;
        await _chatRepository.UpdateSessionName(chatId, newName);
    }

    public async Task DeleteChatHistory(Guid userId, Guid chatId)
    {
        var history = await GetChatHistoryById(userId, chatId);
        if (history == null) return;
        await _chatRepository.DeleteChatHistory(chatId);
    }
}