using Microsoft.EntityFrameworkCore;
using StockTraderBackend.Data;

namespace StockTraderBackend.Chat;

public class ChatRepository
{
    private readonly AppDbContext _db;

    public ChatRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ChatHistory> CreateChatHistory(Guid userId, string sessionName)
    {
        var c = new ChatHistory(userId, sessionName);
        _db.ChatHistories.Add(c);
        await _db.SaveChangesAsync();
        return c;
    }

    public async Task<ChatHistory?> GetChatHistoryById(Guid chatId)
    {
        return await _db.ChatHistories
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(c => c.Id == chatId);
    }

    public async Task<List<ChatHistory>> GetAllChatHistoriesForUser(Guid userId)
    {
        return await _db.ChatHistories
            .Include(c => c.Messages)
            .Where(c => c.UserId == userId)
            .ToListAsync();
    }

    public async Task<ChatHistory?> AddMessageToHistory(ChatMessage message, Guid chatId)
    {
        _db.ChatMessages.Add(message);
        await _db.SaveChangesAsync();
        return await GetChatHistoryById(chatId);
    }

    public async Task<ChatHistory?> RemoveMessageFromHistory(ChatMessage message, Guid chatId)
    {
        _db.ChatMessages.Remove(message);
        await _db.SaveChangesAsync();
        return await GetChatHistoryById(chatId);
    }

    public async Task<ChatMessage?> GetChatMessageById(Guid messageId)
    {
        return await _db.ChatMessages
            .FirstOrDefaultAsync(m => m.Id == messageId);
    }

    public async Task UpdateSessionName(Guid chatId, string newName)
    {
        var history = await _db.ChatHistories.FindAsync(chatId);
        if (history == null) return;
        history.SessionName = newName;
        await _db.SaveChangesAsync();
    }

    public async Task DeleteChatHistory(Guid chatId)
    {
        var history = await _db.ChatHistories.FindAsync(chatId);
        if (history == null) return;
        _db.ChatHistories.Remove(history);
        await _db.SaveChangesAsync();
    }
}