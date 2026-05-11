using Microsoft.AspNetCore.Mvc;
using StockTraderBackend.News.DTOs;
using StockTraderBackend.News.Services;

namespace StockTraderBackend.News.Controllers;

/// <summary>
/// Endpoints for searching and retrieving news articles.
/// </summary>
/// <remarks>
/// Provides access to external news content through the configured news service.
/// 
/// Supported functionality includes:
/// - Searching for articles by keyword
/// - Retrieving top headlines by category
///
/// Results may include user-specific favorite metadata when a valid user ID is provided.
/// </remarks>
[ApiController]
[Route("api/articles")]
[Produces("application/json")]
public class ArticlesController : ControllerBase
{
    private readonly INewsService _newsService;
    private readonly IFavoriteArticleService _favoriteArticleService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ArticlesController"/>.
    /// </summary>
    /// <param name="newsService">Service used to fetch article data from the external news provider.</param>
    /// <param name="favoriteArticleService">Service used for user article favorite operations.</param>
    public ArticlesController(
        INewsService newsService,
        IFavoriteArticleService favoriteArticleService)
    {
        _newsService = newsService;
        _favoriteArticleService = favoriteArticleService;
    }

    /// <summary>
    /// Search for news articles by query string.
    /// </summary>
    /// <remarks>
    /// Searches for articles matching the provided query text.
    /// 
    /// Optional query parameters support language, country, paging, and result count.
    /// A user ID is also supplied so returned articles can include any user-specific
    /// favorite state if supported by the service layer.
    /// </remarks>
    /// <param name="q">The search query text.</param>
    /// <param name="userId">The ID of the user requesting the articles.</param>
    /// <param name="lang">The article language code. Default is "en".</param>
    /// <param name="country">The country code used for article filtering. Default is "us".</param>
    /// <param name="page">The page number for paginated results. Default is 1.</param>
    /// <param name="max">The maximum number of results to return. Default is 10.</param>
    /// <returns>A list of matching articles.</returns>
    /// <response code="200">Articles retrieved successfully.</response>
    /// <response code="400">The search query was missing or invalid.</response>
    [HttpGet("search")]
    [ProducesResponseType(typeof(List<ArticleResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<ArticleResponseDto>>> Search(
        [FromQuery] string q,
        [FromQuery] Guid userId,
        [FromQuery] string lang = "en",
        [FromQuery] string country = "us",
        [FromQuery] int page = 1,
        [FromQuery] int max = 10)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest("Query 'q' is required.");

        var articles = await _newsService.SearchArticlesAsync(q, lang, country, page, max, userId);
        return Ok(articles);
    }

    /// <summary>
    /// Get top news headlines.
    /// </summary>
    /// <remarks>
    /// Returns top headlines for the requested category, language, and country.
    /// 
    /// A user ID is also supplied so returned articles can include any user-specific
    /// favorite state if supported by the service layer.
    /// </remarks>
    /// <param name="userId">The ID of the user requesting the headlines.</param>
    /// <param name="category">The news category to retrieve. Default is "business".</param>
    /// <param name="lang">The article language code. Default is "en".</param>
    /// <param name="country">The country code used for article filtering. Default is "us".</param>
    /// <param name="page">The page number for paginated results. Default is 1.</param>
    /// <param name="max">The maximum number of results to return. Default is 10.</param>
    /// <returns>A list of headline articles.</returns>
    /// <response code="200">Headlines retrieved successfully.</response>
    /// <response code="400">The request was invalid.</response>
    [HttpGet("headlines")]
    [ProducesResponseType(typeof(List<ArticleResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<List<ArticleResponseDto>>> Headlines(
        [FromQuery] Guid userId,
        [FromQuery] string category = "business",
        [FromQuery] string lang = "en",
        [FromQuery] string country = "us",
        [FromQuery] int page = 1,
        [FromQuery] int max = 10)
    {
        var articles = await _newsService.GetHeadlinesAsync(category, lang, country, page, max, userId);
        return Ok(articles);
    }
}