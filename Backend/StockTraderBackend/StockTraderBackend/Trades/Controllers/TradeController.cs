using Microsoft.AspNetCore.Mvc;
using StockTraderBackend.Trades.DTOs;
using StockTraderBackend.Trades.Services;

namespace StockTraderBackend.Trades.Controllers;

/// <summary>
/// Endpoints for executing virtual trades on a portfolio.
/// </summary>
/// <remarks>
/// Handles buy and sell trade requests against a simulated portfolio.
/// No real financial transactions are performed.
/// </remarks>
[ApiController]
[Route("trade")]
[Produces("application/json")]
public class TradeController(ITradeService tradeService) : ControllerBase
{

    /// <summary>
    /// Execute a buy or sell trade on a portfolio.
    /// </summary>
    /// <remarks>
    /// Validates the trade request, fetches the current price, updates the portfolio
    /// cash balance and holdings, and logs the trade record.
    ///
    /// Returns the updated portfolio state after the trade is applied.
    /// </remarks>
    /// <param name="request">Trade payload containing ticker, quantity, and trade type.</param>
    /// <returns>The updated <see cref="PortfolioDTO"/> reflecting the trade.</returns>
    /// <response code="200">Trade executed successfully.</response>
    /// <response code="400">Invalid trade request (e.g. insufficient funds or shares).</response>
    /// <response code="404">Portfolio not found.</response>
    /// <response code="500">Unexpected server error.</response>
    [HttpPost("")]
    public async Task<IActionResult> PerformTrade([FromBody] TradeRequest request)
    {
        try
        {
            var result = await tradeService.handleTrade(request.portfolioId, request);
            return Ok(result);
        }
        catch (KeyNotFoundException e)
        {
            return NotFound(e.Message);
        }
        catch (InvalidOperationException e)
        {
            return BadRequest(e.Message);
        }
        catch (ArgumentException e)
        {
            return BadRequest(e.Message);
        }
        catch (Exception e)
        {
            return StatusCode(500, e.Message);
        }
    }

}