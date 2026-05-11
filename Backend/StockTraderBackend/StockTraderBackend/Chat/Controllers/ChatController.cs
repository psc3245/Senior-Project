using Microsoft.AspNetCore.Mvc;

namespace StockTraderBackend.Chat;

/// <summary>
/// Endpoints for managing chat histories and messages.
/// </summary>
/// <remarks>
/// Provides REST endpoints for creating, retrieving, updating, and deleting
/// chat histories. Real-time messaging is handled via the SignalR ChatHub at /chat.
/// </remarks>
[ApiController]
[Route("api/chat")]
[Produces("application/json")]
public class ChatController : ControllerBase
{
    private readonly ChatService _chatService;

    /// <summary>
    /// Initializes a new instance of <see cref="ChatController"/>.
    /// </summary>
    /// <param name="chatService">Service for managing chat histories.</param>
    public ChatController(ChatService chatService)
    {
        _chatService = chatService;
    }

    /// <summary>
    /// Get all chat histories for a user.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <returns>A list of chat histories for the user.</returns>
    /// <response code="200">Returns the list of chat histories.</response>
    [HttpGet("{userId}")]
    [ProducesResponseType(typeof(List<ChatHistory>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ChatHistory>>> GetAllChatHistories([FromRoute] Guid userId)
    {
        var histories = await _chatService.GetAllChatHistoriesForUser(userId);
        return Ok(histories);
    }

    /// <summary>
    /// Get a specific chat history by ID.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <param name="chatId">The chat history's unique identifier.</param>
    /// <returns>The chat history with all messages.</returns>
    /// <response code="200">Returns the chat history.</response>
    /// <response code="401">User does not own this chat history.</response>
    /// <response code="404">Chat history not found.</response>
    [HttpGet("{userId}/{chatId}")]
    [ProducesResponseType(typeof(ChatHistory), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChatHistory>> GetChatHistory(
        [FromRoute] Guid userId,
        [FromRoute] Guid chatId)
    {
        try
        {
            var history = await _chatService.GetChatHistoryById(userId, chatId);
            if (history == null) return NotFound();
            return Ok(history);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }

    /// <summary>
    /// Create a new chat history.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <param name="sessionName">The name for the new chat session.</param>
    /// <returns>The newly created chat history.</returns>
    /// <response code="201">Chat history created successfully.</response>
    [HttpPost("{userId}")]
    [ProducesResponseType(typeof(ChatHistory), StatusCodes.Status201Created)]
    public async Task<ActionResult<ChatHistory>> CreateChatHistory(
        [FromRoute] Guid userId,
        [FromBody] string sessionName)
    {
        var history = await _chatService.CreateChatHistory(userId, sessionName);
        return CreatedAtAction(nameof(GetChatHistory), new { userId, chatId = history.Id }, history);
    }

    /// <summary>
    /// Update the session name of a chat history.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <param name="chatId">The chat history's unique identifier.</param>
    /// <param name="newName">The new session name.</param>
    /// <response code="204">Session name updated successfully.</response>
    /// <response code="401">User does not own this chat history.</response>
    /// <response code="404">Chat history not found.</response>
    [HttpPut("{userId}/{chatId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> UpdateSessionName(
        [FromRoute] Guid userId,
        [FromRoute] Guid chatId,
        [FromBody] string newName)
    {
        try
        {
            await _chatService.UpdateSessionName(userId, chatId, newName);
            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }

    /// <summary>
    /// Delete a chat history.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <param name="chatId">The chat history's unique identifier.</param>
    /// <response code="204">Chat history deleted successfully.</response>
    /// <response code="401">User does not own this chat history.</response>
    /// <response code="404">Chat history not found.</response>
    [HttpDelete("{userId}/{chatId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteChatHistory(
        [FromRoute] Guid userId,
        [FromRoute] Guid chatId)
    {
        try
        {
            await _chatService.DeleteChatHistory(userId, chatId);
            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
    }
}