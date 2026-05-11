namespace StockTraderBackend.Assets.Controllers
{
    using global::Microsoft.AspNetCore.Mvc;
    using StockTraderBackend.Assets.Symbols.DTOs;
    using StockTraderBackend.Assets.Symbols.Service;

    /// <summary>
    /// Symbol endpoints for retrieving supported stock and company information.
    /// </summary>
    /// <remarks>
    /// Provides read-only access to symbol metadata stored in the local database.
    ///
    /// Supported operations:
    /// - Retrieve all symbols
    /// - Retrieve a symbol by ticker
    /// - Retrieve a symbol by company name
    /// - Retrieve all company names
    ///
    /// Notes:
    /// - These endpoints only return data that already exists in the local database.
    /// - No live provider calls are made.
    /// </remarks>
    [ApiController]
    [Route("api/symbols")]
    [Produces("application/json")]
    public class SymbolsController : ControllerBase
    {
        private readonly ISymbolsService _service;

        /// <summary>
        /// Initializes a new instance of the <see cref="SymbolsController"/>.
        /// </summary>
        /// <param name="service">Service responsible for retrieving symbol data.</param>
        public SymbolsController(ISymbolsService service)
        {
            _service = service;
        }

        /// <summary>
        /// Retrieve all supported symbols.
        /// </summary>
        /// <remarks>
        /// Returns all symbols currently stored in the database.
        /// </remarks>
        /// <param name="ct">Cancellation token for the request.</param>
        /// <returns>A list of all supported symbols.</returns>
        /// <response code="200">Symbols retrieved successfully.</response>
        [HttpGet(Name = "GetAllSymbols")]
        [ProducesResponseType(typeof(List<SymbolResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllSymbols(CancellationToken ct = default)
        {
            var resp = await _service.GetAllSymbolsAsync(ct);
            return Ok(resp);
        }

        /// <summary>
        /// Retrieve stock info by ticker symbol.
        /// </summary>
        /// <remarks>
        /// Returns the symbol record for the specified ticker.
        /// </remarks>
        /// <param name="ticker">Ticker symbol to search for.</param>
        /// <param name="ct">Cancellation token for the request.</param>
        /// <returns>The matching symbol record.</returns>
        /// <response code="200">Symbol retrieved successfully.</response>
        /// <response code="404">No symbol was found for the provided ticker.</response>
        [HttpGet("ticker/{ticker}", Name = "GetSymbolByTicker")]
        [ProducesResponseType(typeof(SymbolResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetSymbolByTicker(
            [FromRoute] string ticker,
            CancellationToken ct = default)
        {
            var resp = await _service.GetSymbolByTickerAsync(ticker, ct);

            if (resp is null)
            {
                return NotFound(new { message = $"No symbol found for ticker '{ticker}'." });
            }

            return Ok(resp);
        }

        /// <summary>
        /// Retrieve stock info by company name.
        /// </summary>
        /// <remarks>
        /// Returns the best matching symbol record for the specified company name.
        ///
        /// If an exact company name match is not found, the closest available match may be returned.
        /// </remarks>
        /// <param name="name">Company name to search for.</param>
        /// <param name="ct">Cancellation token for the request.</param>
        /// <returns>The best matching symbol record.</returns>
        /// <response code="200">A matching symbol was retrieved successfully.</response>
        /// <response code="404">No suitable symbol was found for the provided company name.</response>
        [HttpGet("company", Name = "GetSymbolByCompanyName")]
        [ProducesResponseType(typeof(SymbolResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetSymbolByCompanyName(
            [FromQuery] string name,
            CancellationToken ct = default)
        {
            var resp = await _service.GetSymbolByCompanyNameAsync(name, ct);

            if (resp is null)
            {
                return NotFound(new { message = $"No symbol found matching company name '{name}'." });
            }

            return Ok(resp);
        }


        /// <summary>
        /// Retrieve all company names.
        /// </summary>
        /// <remarks>
        /// Returns a list of all company names currently stored in the database.
        /// </remarks>
        /// <param name="ct">Cancellation token for the request.</param>
        /// <returns>A list of company names.</returns>
        /// <response code="200">Company names retrieved successfully.</response>
        [HttpGet("company-names", Name = "GetAllCompanyNames")]
        [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllCompanyNames(CancellationToken ct = default)
        {
            var resp = await _service.GetAllCompanyNamesAsync(ct);
            return Ok(resp);
        }
    }
}