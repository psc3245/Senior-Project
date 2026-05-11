using StockTraderBackend.Assets.PriceBar.DTOs;

namespace StockTraderBackend.Assets.PriceBar.Services
{
    public interface IPriceBarsService
    {
        Task<PriceBarsResponse> GetDailyBarsAsync(
            string symbol,
            string tf,
            DateOnly? from,
            DateOnly? to,
            int limit,
            CancellationToken ct);
    }

}
