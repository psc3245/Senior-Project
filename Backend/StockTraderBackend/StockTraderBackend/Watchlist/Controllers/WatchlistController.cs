using Microsoft.AspNetCore.Mvc;
using StockTraderBackend.Watchlist.DTOs;
using StockTraderBackend.Watchlist.Service;

namespace StockTraderBackend.Watchlist.Controllers;

/// <summary>
/// Endpoints for managing user watchlists.
/// </summary>
/// <remarks>
/// Provides functionality for creating, retrieving, and deleting watchlists.
/// 
/// Supported operations include:
/// - Creating new watchlists for users
/// - Retrieving all watchlists for a specific user
/// - Retrieving a single watchlist by ID
/// - Deleting watchlists
/// </remarks>
[ApiController]
[Route("api/watchlists")]
[Produces("application/json")]
public class WatchlistController : ControllerBase
{
    private readonly IWatchlistService _watchlistService;

    /// <summary>
    /// Initializes a new instance of the <see cref="WatchlistController"/>.
    /// </summary>
    /// <param name="watchlistService">Service used to manage watchlist operations.</param>
    public WatchlistController(IWatchlistService watchlistService)
    {
        _watchlistService = watchlistService;
    }

    /// <summary>
    /// Get all watchlists for a user.
    /// </summary>
    /// <remarks>
    /// Retrieves all watchlists belonging to the specified user, including their associated stocks.
    /// Watchlists are ordered by creation date.
    /// </remarks>
    /// <param name="userId">The ID of the user whose watchlists to retrieve.</param>
    /// <returns>A list of watchlists with their stocks.</returns>
    /// <response code="200">Watchlists retrieved successfully.</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<Watchlist>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserWatchlists([FromQuery] Guid userId)
    {
        var watchlists = await _watchlistService.GetUserWatchlists(userId);
        return Ok(watchlists);
    }

    /// <summary>
    /// Get a specific watchlist by ID.
    /// </summary>
    /// <remarks>
    /// Retrieves a single watchlist with all its stocks and associated symbol information.
    /// </remarks>
    /// <param name="id">The watchlist ID.</param>
    /// <returns>The requested watchlist with stocks.</returns>
    /// <response code="200">Watchlist retrieved successfully.</response>
    /// <response code="404">Watchlist not found.</response>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Watchlist), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWatchlist(Guid id)
    {
        var watchlist = await _watchlistService.GetWatchlistById(id);
        if (watchlist == null)
            return NotFound();
        return Ok(watchlist);
    }

    /// <summary>
    /// Create a new watchlist.
    /// </summary>
    /// <remarks>
    /// Creates a new empty watchlist for the specified user with the given name.
    /// </remarks>
    /// <param name="request">The watchlist creation request containing user ID and name.</param>
    /// <returns>The newly created watchlist.</returns>
    /// <response code="201">Watchlist created successfully.</response>
    /// <response code="400">The request was invalid.</response>
    [HttpPost]
    [ProducesResponseType(typeof(Watchlist), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateWatchlist([FromBody] CreateWatchlistRequest request)
    {
        var watchlist = await _watchlistService.CreateWatchlist(request.UserId, request.Name);
        return CreatedAtAction(nameof(GetWatchlist), new { id = watchlist.Id }, watchlist);
    }

    /// <summary>
    /// Delete a watchlist.
    /// </summary>
    /// <remarks>
    /// Permanently deletes the specified watchlist and all its associated stocks.
    /// </remarks>
    /// <param name="id">The ID of the watchlist to delete.</param>
    /// <returns>No content on successful deletion.</returns>
    /// <response code="204">Watchlist deleted successfully.</response>
    /// <response code="404">Watchlist not found.</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteWatchlist(Guid id)
    {
        await _watchlistService.DeleteWatchlist(id);
        return NoContent();
    }
}