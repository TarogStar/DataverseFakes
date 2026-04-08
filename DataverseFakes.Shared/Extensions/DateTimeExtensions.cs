using System;
using System.Globalization;

namespace FakeXrmEasy.Extensions
{
    /// <summary>
    /// Provides extension methods for <see cref="DateTime"/> to support date-based filtering operations
    /// commonly used in FetchXML and QueryExpression conditions.
    /// </summary>
    public static class DateTimeExtensions
    {
        /// <summary>
        /// Calculates the date for a specific day of the week within a given week number of the year.
        /// </summary>
        /// <param name="dateTime">The reference date used to determine the year.</param>
        /// <param name="week">The week number of the year (1-based).</param>
        /// <param name="dayOfWeek">The desired day of the week.</param>
        /// <returns>A <see cref="DateTime"/> representing the specified day in the specified week.</returns>
        public static DateTime ToDayOfWeek(this DateTime dateTime, Int32 week, DayOfWeek dayOfWeek)
        {
            DateTime startOfYear = dateTime.AddDays(1 - dateTime.DayOfYear);
            return startOfYear.AddDays(7 * (week - 2) + ((dayOfWeek - startOfYear.DayOfWeek + 7) % 7));
        }

        /// <summary>
        /// Calculates the date for a specific day of the week, offset by a number of weeks from the current date.
        /// Uses the current culture's calendar settings for week calculation.
        /// </summary>
        /// <param name="dateTime">The reference date.</param>
        /// <param name="deltaWeek">The number of weeks to offset (positive for future, negative for past).</param>
        /// <param name="dayOfWeek">The desired day of the week.</param>
        /// <returns>A <see cref="DateTime"/> representing the specified day in the offset week.</returns>
        public static DateTime ToDayOfDeltaWeek(this DateTime dateTime, Int32 deltaWeek, DayOfWeek dayOfWeek)
            => dateTime.ToDayOfWeek(CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(dateTime
                , CultureInfo.CurrentCulture.DateTimeFormat.CalendarWeekRule
                , CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek) + deltaWeek, dayOfWeek);

        /// <summary>
        /// Calculates the last day of a week, offset by a number of weeks from the reference date.
        /// The last day is determined by the current culture's first day of week setting plus 6 days.
        /// </summary>
        /// <param name="dateTime">The reference date.</param>
        /// <param name="deltaWeek">The number of weeks to offset (default is 0 for current week).</param>
        /// <returns>A <see cref="DateTime"/> representing the last day of the offset week.</returns>
        public static DateTime ToLastDayOfDeltaWeek(this DateTime dateTime, Int32 deltaWeek = 0)
            => dateTime.ToDayOfDeltaWeek(deltaWeek, CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek).AddDays(6);

        /// <summary>
        /// Calculates the first day of a week, offset by a number of weeks from the reference date.
        /// The first day is determined by the current culture's first day of week setting.
        /// </summary>
        /// <param name="dateTime">The reference date.</param>
        /// <param name="deltaWeek">The number of weeks to offset (default is 0 for current week).</param>
        /// <returns>A <see cref="DateTime"/> representing the first day of the offset week.</returns>
        public static DateTime ToFirstDayOfDeltaWeek(this DateTime dateTime, Int32 deltaWeek = 0)
            => dateTime.ToDayOfDeltaWeek(deltaWeek, CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek);

        /// <summary>
        /// Calculates the first day of a specified month in the same year as the reference date.
        /// </summary>
        /// <param name="dateTime">The reference date.</param>
        /// <param name="month">The target month (1-12).</param>
        /// <returns>A <see cref="DateTime"/> representing the first day of the specified month.</returns>
        public static DateTime ToFirstDayOfMonth(this DateTime dateTime, Int32 month)
            => dateTime.AddDays(1 - dateTime.Day).AddMonths(month - dateTime.Month);

        /// <summary>
        /// Calculates the first day of the current month based on the reference date.
        /// </summary>
        /// <param name="dateTime">The reference date.</param>
        /// <returns>A <see cref="DateTime"/> representing the first day of the current month.</returns>
        public static DateTime ToFirstDayOfMonth(this DateTime dateTime)
            => dateTime.ToFirstDayOfMonth(dateTime.Month);

        /// <summary>
        /// Calculates the last day of a specified month. Handles months greater than 12 by rolling into subsequent years.
        /// </summary>
        /// <param name="dateTime">The reference date.</param>
        /// <param name="month">The target month (1-12, or higher for months in subsequent years).</param>
        /// <returns>A <see cref="DateTime"/> representing the last day of the specified month.</returns>
        public static DateTime ToLastDayOfMonth(this DateTime dateTime, Int32 month)
        {
            Int32 addYears = month > 12 ? month % 12 : 0;
            month = month - 12 * addYears;
            return dateTime
                .AddDays(CultureInfo.CurrentCulture.Calendar.GetDaysInMonth(dateTime.Year + addYears, month) - dateTime.Day)
                .AddMonths(month - dateTime.Month).AddYears(addYears);
        }

        /// <summary>
        /// Calculates the last day of the current month based on the reference date.
        /// </summary>
        /// <param name="dateTime">The reference date.</param>
        /// <returns>A <see cref="DateTime"/> representing the last day of the current month.</returns>
        public static DateTime ToLastDayOfMonth(this DateTime dateTime)
            => dateTime.ToLastDayOfMonth(dateTime.Month);
    }
}
