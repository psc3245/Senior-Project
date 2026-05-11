using System.Text.Json;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using StockTraderBackend.Chat.Tools;
using StockTraderBackend.Portfolios;
using StockTraderBackend.Users;

namespace StockTraderBackend.Chat.AI;

public class AIClient : IAIClient
{
    private readonly ChatClient _client;
    private readonly AIOptions _options;
    private readonly ILogger<AIClient> _logger;
    private readonly LlmToolDispatcher _toolDispatcher;

    public AIClient(IOptions<AIOptions> options,ILogger<AIClient> logger,LlmToolDispatcher toolDispatcher)
    {
        _options = options.Value;
        _logger = logger;
        _toolDispatcher = toolDispatcher;

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new InvalidOperationException("AI ApiKey missing");

        _client = new ChatClient(
            model: _options.Model,
            apiKey: _options.ApiKey
        );
    }
    private List<ChatTool> BuildTools()
    {
        return _toolDispatcher.Tools.Select(t =>
            ChatTool.CreateFunctionTool(
                functionName: t.Name,
                functionDescription: t.Description,
                functionParameters: BinaryData.FromObjectAsJson(t.JsonSchema)
            )
        ).ToList();
    }
    public async Task<string> SendMessageAsync(Guid userId,
        List<ChatMessage> history,
        string newMessage)
    {
        try
        {
            var messages = new List<OpenAI.Chat.ChatMessage>();

            messages.Add(
                OpenAI.Chat.ChatMessage.CreateSystemMessage(
                    _options.SystemPrompt
                )
            );

            var trimmedHistory = history
                .OrderBy(m => m.Timestamp)
                .TakeLast(_options.MaxHistoryMessages);

            foreach (var msg in trimmedHistory)
            {
                if (msg.Role == "user")
                {
                    messages.Add(
                        OpenAI.Chat.ChatMessage.CreateUserMessage(msg.Message)
                    );
                }
                else if (msg.Role == "assistant")
                {
                    messages.Add(
                        OpenAI.Chat.ChatMessage.CreateAssistantMessage(msg.Message)
                    );
                }
            }

            messages.Add(
                OpenAI.Chat.ChatMessage.CreateUserMessage(newMessage)
            );


            var options = new ChatCompletionOptions();

            foreach (var tool in BuildTools())
            {
                options.Tools.Add(tool);
            }
            while (true)
            {
                var completion = await _client.CompleteChatAsync(messages, options);
                var response = completion.Value;

                if (response.FinishReason == ChatFinishReason.Stop)
                {
                    return response.Content[0].Text.Trim();
                }

                if (response.FinishReason == ChatFinishReason.ToolCalls)
                {
                    messages.Add(new AssistantChatMessage(response));

                    foreach (var toolCall in response.ToolCalls)
                    {
                        _logger.LogInformation("Tool called: {ToolName}", toolCall.FunctionName);
                        var result = await _toolDispatcher.ExecuteAsync(
                        toolCall.FunctionName,
                            userId,
                            JsonDocument.Parse(toolCall.FunctionArguments.ToString()).RootElement,
                            CancellationToken.None
                        );

                        messages.Add(new ToolChatMessage(
                            toolCall.Id,
                            BinaryData.FromObjectAsJson(result).ToString()
                        ));
                    }

                    continue;
                }

                return "Sorry, I couldn't complete the request.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI error");

            return "Sorry, I couldn't generate a response.";
        }
    }
}