using PaperTrails.Api.Models;

namespace PaperTrails.Api.Services
{
    public static class RecurrenceHelper
    {
        public static DateTime NextOccurrence(DateTime lastScheduled, RecurrenceType recurrence)
        {
            return recurrence switch
            {
                RecurrenceType.Weekly => lastScheduled.AddDays(7),
                RecurrenceType.Monthly => SafeAddMonths(lastScheduled, 1),
                _ => lastScheduled
            };
        }

        public static DateTime Advance(DateTime from, RecurrenceType recurrence)
        {
            return recurrence switch
            {
                RecurrenceType.Weekly => from.AddDays(7),
                RecurrenceType.Monthly => SafeAddMonths(from, 1),
                _ => from
            };
        }

        private static DateTime SafeAddMonths(DateTime dt, int months)
        {
            int newMonth = dt.Month + months;
            int years = (newMonth - 1) / 12;
            newMonth = ((newMonth - 1) % 12) + 1;
            int newYear = dt.Year + years;
            int daysInNewMonth = DateTime.DaysInMonth(newYear, newMonth);
            int newDay = Math.Min(dt.Day, daysInNewMonth);
            return new DateTime(newYear, newMonth, newDay, dt.Hour, dt.Minute, dt.Second, dt.Kind);
        }
    }
}
