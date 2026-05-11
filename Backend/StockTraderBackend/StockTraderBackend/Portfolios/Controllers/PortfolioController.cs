using Microsoft.AspNetCore.Mvc;
using StockTraderBackend.Portfolios.DTOs;

namespace StockTraderBackend.Portfolios;

/// <summary>
/// Endpoints for managing user portfolios.
/// </summary>
/// <remarks>
/// Provides read and delete operations for portfolios.
/// Portfolio creation is handled automatically on user registration.
/// </remarks>
[ApiController]
[Route("portfolio")]
[Produces("application/json")]
public class PortfolioController(IPortfolioService portfolioService) : ControllerBase
{
    /// <summary>
    /// Get all portfolios.
    /// </summary>
    /// <remarks>
    /// Returns all portfolios currently stored. Intended for development and testing only.
    /// </remarks>
    /// <returns>A list of all <see cref="PortfolioDTO"/> objects.</returns>
    /// <response code="200">Returns the list of portfolios.</response>
    [HttpGet]
    public async Task<ActionResult<ICollection<PortfolioDTO>>> GetAllPortfolios(CancellationToken ct)
    {
        var portfolios = await portfolioService.getAllPortfolios(ct);
        return Ok(portfolios);
    }

    /// <summary>
    /// Get a portfolio by user ID.
    /// </summary>
    /// <remarks>
    /// Returns the portfolio associated with the given user, including
    /// current holdings and computed holding value.
    /// </remarks>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <returns>The <see cref="PortfolioDTO"/> for the given user.</returns>
    /// <response code="200">Returns the portfolio.</response>
    /// <response code="404">No portfolio found for the given user ID.</response>
    [HttpGet("{userId:guid}")]
    public async Task<ActionResult<PortfolioDTO>> GetPortfolioByUserId(Guid userId, CancellationToken ct)
    {
        try
        {
            var dto = await portfolioService.getPortfolioByUserId(userId, ct);
            return Ok(dto);
        }
        catch (KeyNotFoundException e)
        {
            return NotFound(e.Message);
        }
        catch (Exception e)
        {
            return StatusCode(500, e.Message);
        }
    }

    /// <summary>
    /// Delete a portfolio by portfolio ID.
    /// </summary>
    /// <remarks>
    /// Permanently removes the portfolio and all associated holdings and trades.
    /// This action cannot be undone.
    /// </remarks>
    /// <param name="portfolioId">The unique identifier of the portfolio to delete.</param>
    /// <returns>200 OK if deleted successfully.</returns>
    /// <response code="200">Portfolio deleted successfully.</response>
    /// <response code="404">Portfolio not found.</response>
    [HttpDelete("{portfolioId:guid}")]
    public async Task<IActionResult> DeletePortfolio(Guid portfolioId, CancellationToken ct)
    {
        try
        {
            await portfolioService.deletePortfolio(portfolioId, ct);
            return Ok();
        }
        catch (KeyNotFoundException e)
        {
            return NotFound(e.Message);
        }
        catch (Exception e)
        {
            return StatusCode(500, e.Message);
        }
    }
}
