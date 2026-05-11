namespace StockTraderBackend.MarketData.Calendar
{
    public class DefaultMarketCalendarService : IMarketCalendarService
    {
        public bool IsTradingDay(DateTime date)
        {
            var d = date.Date;

            if (d.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                return false;

            if (IsUsMarketHoliday(d))
                return false;

            return true;
        }

        public DateTime GetMostRecentTradingDay(DateTime date)
        {
            var cursor = date.Date;

            while (!IsTradingDay(cursor))
            {
                cursor = cursor.AddDays(-1);
            }

            return cursor;
        }

        public int CountTradingDaysBetweenExclusive(DateTime start, DateTime end)
        {
            var count = 0;
            var cursor = start.Date.AddDays(1);
            var endDate = end.Date;

            while (cursor < endDate)
            {
                if (IsTradingDay(cursor))
                {
                    count++;
                }

                cursor = cursor.AddDays(1);
            }

            return count;
        }

        private static bool IsUsMarketHoliday(DateTime date)
        {
            var year = date.Year;

            return date == NewYearsHolidayObserved(year)
                || date == NthWeekdayOfMonth(year, 1, DayOfWeek.Monday, 3)   // MLK Day
                || date == NthWeekdayOfMonth(year, 2, DayOfWeek.Monday, 3)   // Presidents Day
                || date == GoodFriday(year)
                || date == LastWeekdayOfMonth(year, 5, DayOfWeek.Monday)     // Memorial Day
                || date == JuneteenthObserved(year)
                || date == IndependenceDayObserved(year)
                || date == NthWeekdayOfMonth(year, 9, DayOfWeek.Monday, 1)   // Labor Day
                || date == NthWeekdayOfMonth(year, 11, DayOfWeek.Thursday, 4) // Thanksgiving
                || date == ChristmasObserved(year);
        }

        private static DateTime NewYearsHolidayObserved(int year)
        {
            return ObserveFixedHoliday(new DateTime(year, 1, 1));
        }

        private static DateTime JuneteenthObserved(int year)
        {
            return ObserveFixedHoliday(new DateTime(year, 6, 19));
        }

        private static DateTime IndependenceDayObserved(int year)
        {
            return ObserveFixedHoliday(new DateTime(year, 7, 4));
        }

        private static DateTime ChristmasObserved(int year)
        {
            return ObserveFixedHoliday(new DateTime(year, 12, 25));
        }

        private static DateTime ObserveFixedHoliday(DateTime holiday)
        {
            return holiday.DayOfWeek switch
            {
                DayOfWeek.Saturday => holiday.AddDays(-1),
                DayOfWeek.Sunday => holiday.AddDays(1),
                _ => holiday
            };
        }

        private static DateTime NthWeekdayOfMonth(
            int year,
            int month,
            DayOfWeek dayOfWeek,
            int occurrence)
        {
            var first = new DateTime(year, month, 1);
            var offset = ((int)dayOfWeek - (int)first.DayOfWeek + 7) % 7;
            return first.AddDays(offset + (occurrence - 1) * 7);
        }

        private static DateTime LastWeekdayOfMonth(
            int year,
            int month,
            DayOfWeek dayOfWeek)
        {
            var last = new DateTime(year, month, DateTime.DaysInMonth(year, month));

            while (last.DayOfWeek != dayOfWeek)
            {
                last = last.AddDays(-1);
            }

            return last;
        }

        private static DateTime GoodFriday(int year)
        {
            return EasterSunday(year).AddDays(-2);
        }

        private static DateTime EasterSunday(int year)
        {
            // Anonymous Gregorian algorithm
            int a = year % 19;
            int b = year / 100;
            int c = year % 100;
            int d = b / 4;
            int e = b % 4;
            int f = (b + 8) / 25;
            int g = (b - f + 1) / 3;
            int h = (19 * a + b - d - g + 15) % 30;
            int i = c / 4;
            int k = c % 4;
            int l = (32 + 2 * e + 2 * i - h - k) % 7;
            int m = (a + 11 * h + 22 * l) / 451;
            int month = (h + l - 7 * m + 114) / 31;
            int day = ((h + l - 7 * m + 114) % 31) + 1;

            return new DateTime(year, month, day);
        }
    }
}