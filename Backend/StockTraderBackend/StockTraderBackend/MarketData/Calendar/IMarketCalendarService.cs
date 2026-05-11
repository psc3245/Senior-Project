namespace StockTraderBackend.MarketData.Calendar
{
    public interface IMarketCalendarService
    {
        bool IsTradingDay(DateTime date);
        DateTime GetMostRecentTradingDay(DateTime date);
        int CountTradingDaysBetweenExclusive(DateTime start, DateTime end);
    }
}