using System.Text.Json;
using StockTraderBackend.Chat.LLMTools;

namespace StockTraderBackend.Chat.Tools;

public sealed class LlmToolDispatcher
{
    private readonly Dictionary<string, ILlmTool> _tools;

    public LlmToolDispatcher(IEnumerable<ILlmTool> tools)
    {
        _tools = tools.ToDictionary(t => t.Name);
    }

    public IReadOnlyCollection<ILlmTool> Tools => _tools.Values;

    public async Task<object> ExecuteAsync(
        string toolName,
        Guid userId,
        JsonElement arguments,
        CancellationToken ct)
    {
        if (!_tools.TryGetValue(toolName, out var tool))
            throw new InvalidOperationException($"Unknown LLM tool: {toolName}");

        return await tool.ExecuteAsync(userId, arguments, ct);
    }
}