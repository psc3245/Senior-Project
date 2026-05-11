using Microsoft.AspNetCore.Mvc;
using StockTraderBackend.News.Services;
using StockTraderBackend.News.DTOs;

namespace StockTraderBackend.News.Controllers;

/// <summary>
/// Endpoints for managing a user's favorite news articles.
/// </summary>
/// <remarks>
/// Provides functionality to:
/// - Add an article to a user's favorites
/// - Retrieve all favorited articles for a user
/// - Remove an article from a user's favorites
///
/// Favorite relationships are tied to a specific user and article.
/// </remarks>
[ApiController]
[Route("api/articles/favorites")]
[Produces("application/json")]
public class FavoriteArticleController : ControllerBase
{
    private readonly IFavoriteArticleService _favoriteService;

    /// <summary>
    /// Initializes a new instance of the <see cref="FavoriteArticleController"/>.
    /// </summary>
    /// <param name="favoriteService">Service used to manage favorited articles.</param>
    public FavoriteArticleController(IFavoriteArticleService favoriteService)
    {
        _favoriteService = favoriteService;
    }

    /// <summary>
    /// Add an article to a user's favorites.
    /// </summary>
    /// <remarks>
    /// Creates a favorite association between the specified user and article.
    /// 
    /// The article is identified by route parameter, and the user is identified
    /// by query parameter.
    /// </remarks>
    /// <param name="articleId">The ID of the article to favorite.</param>
    /// <param name="userId">The ID of the user favoriting the article.</param>
    /// <returns>No content other than a successful status response.</returns>
    /// <response code="200">Article was successfully added to favorites.</response>
    /// <response code="400">The request was invalid.</response>
    [HttpPost("{articleId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Favorite(
        [FromRoute] Guid articleId,
        [FromQuery] Guid userId)
    {
        await _favoriteService.FavoriteArticleAsync(userId, articleId);
        return Ok();
    }

    /// <summary>
    /// Get all favorited articles for a user.
    /// </summary>
    /// <remarks>
    /// Returns a list of article response DTOs representing the articles
    /// the specified user has marked as favorites.
    /// </remarks>
    /// <param name="userId">The ID of the user whose favorites should be returned.</param>
    /// <returns>A list of the user's favorited articles.</returns>
    /// <response code="200">Favorites retrieved successfully.</response>
    /// <response code="400">The request was invalid.</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<ArticleResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<ArticleResponseDto>>> GetFavorites([FromQuery] Guid userId)
    {
        var favorites = await _favoriteService.GetFavoritesAsync(userId);
        return Ok(favorites);
    }

    /// <summary>
    /// Remove an article from a user's favorites.
    /// </summary>
    /// <remarks>
    /// Deletes the favorite association between the specified user and article.
    /// </remarks>
    /// <param name="articleId">The ID of the article to remove from favorites.</param>
    /// <param name="userId">The ID of the user removing the favorite.</param>
    /// <returns>No content other than a successful status response.</returns>
    /// <response code="200">Favorite removed successfully.</response>
    /// <response code="400">The request was invalid.</response>
    [HttpDelete("{articleId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteFavorite(
        [FromRoute] Guid articleId,
        [FromQuery] Guid userId)
    {
        await _favoriteService.RemoveFavoriteAsync(userId, articleId);
        return Ok();
    }
}