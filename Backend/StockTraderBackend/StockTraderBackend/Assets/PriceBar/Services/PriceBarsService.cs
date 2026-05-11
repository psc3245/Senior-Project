using StockTraderBackend.Assets.PriceBar.DTOs;
using StockTraderBackend.Assets.Symbols;
using StockTraderBackend.Middleware;
namespace StockTraderBackend.Assets.PriceBar.Services
{
    public sealed class PriceBarsService : IPriceBarsService
    {
        private readonly IPriceBarsRepository _repo;
        private readonly ISymbolsRepository _symbols;

        public PriceBarsService(IPriceBarsRepository repo, ISymbolsRepository symbols)
        {
            _repo = repo;
            _symbols = symbols;
        }

        public async Task<PriceBarsResponse> GetDailyBarsAsync(
            string symbol,
            string tf,
            DateOnly? from,
            DateOnly? to,
            int limit,
            CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(symbol))
                throw new ApiException(400, "symbol is required");

            symbol = symbol.Trim().ToUpperInvariant();

            tf = (tf ?? "1d").Trim().ToLowerInvariant();
            if (tf != "1d")
                throw new ApiException(400, "Only tf=1d is supported right now.");

            if (limit <= 0) limit = 365;
            if (limit > 5000) limit = 5000;

            // resolve symbol id
            var symbolRow = await _symbols.GetByTickerAsync(symbol, ct);
            if (symbolRow == null)
                throw new ApiException(404, $"Unknown symbol '{symbol}'");

            var bars = await _repo.GetDailyBarsAsync(symbolRow.Id, from, to, limit, ct);
            bars.Reverse();

            return new PriceBarsResponse
            {
                Symbol = symbolRow.Ticker,
                Timeframe = "1d",
                Data = bars.Select(b => new PriceBarDto(
                    DateOnly.FromDateTime(b.TsUtc),
                    b.Open, b.High, b.Low, b.Close, b.Volume
                )).ToList()
            };
        }
    }
}
