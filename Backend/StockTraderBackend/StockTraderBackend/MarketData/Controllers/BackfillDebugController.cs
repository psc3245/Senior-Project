using Microsoft.AspNetCore.Mvc;
using StockTraderBackend.MarketData.Backfill;

namespace StockTraderBackend.Controllers
{
    [ApiController]
    [Route("api/debug/backfill")]
    public sealed class BackfillDebugController : ControllerBase
    {
        private readonly ISymbolBackfillService _symbolBackfillService;
        private readonly IWebHostEnvironment _environment;

        public BackfillDebugController(
            ISymbolBackfillService symbolBackfillService,
            IWebHostEnvironment environment)
        {
            _symbolBackfillService = symbolBackfillService;
            _environment = environment;
        }

        [HttpPost("{symbolId:int}")]
        public async Task<IActionResult> BackfillSymbol(
            int symbolId,
            CancellationToken ct)
        {
            if (!_environment.IsDevelopment())
            {
                return NotFound();
            }

            await _symbolBackfillService.BackfillSymbolAsync(symbolId, ct);

            return Ok(new
            {
                message = $"Backfill completed for symbol {symbolId}"
            });
        }

        [HttpPost("scan")]
        public async Task<IActionResult> ScanAndRepair(CancellationToken ct)
        {
            if (!_environment.IsDevelopment())
            {
                return NotFound();
            }

            await _symbolBackfillService.ScanAndRepairTrackedSymbolsAsync(ct);

            return Ok(new
            {
                message = "Backfill scan completed"
            });
        }
    }
}