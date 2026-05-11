using Microsoft.AspNetCore.SignalR;

namespace StockTraderBackend.Chat;

public class ChatHub : Hub
{
    private readonly ChatService _chatService;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(ChatService chatService, ILogger<ChatHub> logger)
    {
        _chatService = chatService;
        _logger = logger;
    }

    /// <summary>
    /// Sends a message to the AI and returns the response.
    /// </summary>
    /// <remarks>
    /// Client invocation: connection.invoke("SendMessage", userId, chatId, message)
    /// Server callback: "ReceiveMessage" (historyId, message, role, timestamp)
    /// Server callback: "Error" (errorMessage)
    /// </remarks>
    public async Task SendMessage(Guid userId, Guid chatId, string message)
    {
        var history = await _chatService.SendMessage(userId, chatId, message);
        if (history == null)
        {
            await Clients.Caller.SendAsync("Error", "Chat history not found or unauthorized.");
            return;
        }

        var lastMessage = history.Messages.LastOrDefault();
        if (lastMessage != null)
        {
            await Clients.Caller.SendAsync("ReceiveMessage", lastMessage.ChatHistoryId, lastMessage.Message, lastMessage.Role, lastMessage.Timestamp);
        }
    }

    /// <summary>
    /// Creates a new chat history session for a user.
    /// </summary>
    /// <remarks>
    /// Client invocation: connection.invoke("CreateChat", userId, sessionName)
    /// Server callback: "ChatCreated" (chatId, sessionName)
    /// </remarks>
    public async Task CreateChat(Guid userId, string sessionName)
    {
        var history = await _chatService.CreateChatHistory(userId, sessionName);
        await Clients.Caller.SendAsync("ChatCreated", history.Id, history.SessionName);
    }
}