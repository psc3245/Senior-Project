using System.Text.Json;

namespace StockTraderBackend.Chat.LLMTools
{
    public interface ILlmTool
    {
        string Name { get; }
        string Description { get; }
        object JsonSchema { get; }

        Task<object> ExecuteAsync(
            Guid userId,
            JsonElement arguments,
            CancellationToken ct);
    }
}
