using System;
using System.Globalization;

namespace Stoolball.Dates
{
	public class DateFormatter : IDateFormatter
	{
		/// <summary>
		/// Get a date in the format "10am, Sunday 1 January 2000"
		/// </summary>
		/// <param name="dateTimeToFormat">The date and time to display</param>
		/// <param name="currentDateTime">The current date and time, used when displaying relative dates</param>
		/// <param name="dayOfTheWeek">Include Monday, Tuesday, Wednesday etc</param>
		/// <param name="time">Include the time of day</param>
		/// <param name="relativeDate">Refer to "this Sunday" rather than "Sunday 12 June"</param>
		/// <param name="abbreviated">Use Jan, Feb etc rather than January, February etc</param>
		/// <returns></returns>
		public string FormatDate(DateTimeOffset dateTimeToFormat, DateTimeOffset currentDateTime, bool dayOfTheWeek = true, bool time = true, bool relativeDate = true, bool abbreviated = false)
		{
			// Safe to assume visitors to Stoolball England want times expressed in the UK time zone
			var ukTimeZone = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");
			var ukStartTime = TimeZoneInfo.ConvertTimeFromUtc(dateTimeToFormat.UtcDateTime, ukTimeZone);

			if (time)
			{
				return Time(ukStartTime) + TimeDateSeparator(ukStartTime, currentDateTime) + BritishDate(ukStartTime, currentDateTime, dayOfTheWeek, relativeDate, abbreviated);
			}
			else
			{
				return BritishDate(ukStartTime, currentDateTime, dayOfTheWeek, relativeDate, abbreviated);
			}
		}

		/// <summary>
		/// Get a date in the format "10am, Sunday 1 January 2000"
		/// </summary>
		/// <param name="dateTimeToFormat">The date and time to display</param>
		/// <param name="dayOfTheWeek">Include Monday, Tuesday, Wednesday etc</param>
		/// <param name="time">Include the time of day</param>
		/// <param name="relativeDate">Refer to "this Sunday" rather than "Sunday 12 June"</param>
		/// <param name="abbreviated">Use Jan, Feb etc rather than January, February etc</param>
		/// <returns></returns>
		public string FormatDate(DateTimeOffset dateTimeToFormat, bool dayOfTheWeek = true, bool time = true, bool relativeDate = true, bool abbreviated = false)
		{
			return FormatDate(dateTimeToFormat, DateTimeOffset.UtcNow, dayOfTheWeek, time, relativeDate, abbreviated);
		}

		/// <summary>
		/// Gets the time in the format 10am or 10.01am
		/// </summary>
		private static string Time(DateTimeOffset dateTimeToFormat)
		{
			var time = dateTimeToFormat.ToString("h.mmtt", CultureInfo.CurrentCulture)
					.ToLower(CultureInfo.CurrentCulture)
					.Replace(".00", string.Empty);
			if (time == "12pm") { time = "Midday"; }
			else if (time == "12am") { time = "Midnight"; }
			return time;
		}

		/// <summary>
		/// Gets a comma if the date isn't within a week either side of the current date
		/// </summary>
		private static string TimeDateSeparator(DateTimeOffset dateTimeToFormat, DateTimeOffset currentDateTime)
		{
			if (dateTimeToFormat.Date == currentDateTime.Date)
			{
				return " ";
			}
			else if (dateTimeToFormat.Date == (currentDateTime.Date.AddDays(-1)))
			{
				return " ";
			}
			else if (dateTimeToFormat.Date == (currentDateTime.Date.AddDays(1)))
			{
				return " ";
			}
			else if (dateTimeToFormat.Date > (currentDateTime.Date.AddDays(-7)) && dateTimeToFormat.Date < currentDateTime.Date)
			{
				return " ";
			}
			else if (dateTimeToFormat.Date < (currentDateTime.Date.AddDays(6)) && dateTimeToFormat.Date > currentDateTime.Date)
			{
				return " ";
			}
			else
			{
				return ", ";
			}
		}

		/// <summary>
		/// Get a date in the format "Sunday 1 January 2000"
		/// </summary>
		/// <param name="dateTime">The date and time to display</param>
		/// <param name="dayOfTheWeek">Include Monday, Tuesday, Wednesday etc</param>
		/// <param name="relativeDate">Refer to "this Sunday" rather than "Sunday 12 June"</param>
		/// <param name="abbreviated">Use Jan, Feb etc rather than January, February etc</param>
		/// <returns></returns>
		private static string BritishDate(DateTimeOffset dateTimeToFormat, DateTimeOffset currentDateTime, bool dayOfTheWeek = true, bool relativeDate = true, bool abbreviated = false)
		{
			// Format the timestamp according to the user's perception of when it occurred
			var day = string.Empty;
			if (dateTimeToFormat.Date == currentDateTime.Date)
			{
				day = "today";
			}
			else if (dateTimeToFormat.Date == (currentDateTime.Date.AddDays(-1)))
			{
				day = "yesterday";
			}
			else if (dateTimeToFormat.Date == (currentDateTime.Date.AddDays(1)))
			{
				day = "tomorrow";
			}
			else if (dateTimeToFormat.Date > (currentDateTime.Date.AddDays(-7)) && dateTimeToFormat.Date < currentDateTime.Date)
			{
				day = "last " + dateTimeToFormat.ToString(abbreviated ? "ddd " : "dddd ", CultureInfo.CurrentCulture);
			}
			else if (dateTimeToFormat.Date < (currentDateTime.Date.AddDays(6)) && dateTimeToFormat.Date > currentDateTime.Date)
			{
				day = "this " + dateTimeToFormat.ToString(abbreviated ? "ddd " : "dddd ", CultureInfo.CurrentCulture);
			}

			// If that didn't match anything, or if caller specifically asked not to get a relative date, use an absolute date format
			if (string.IsNullOrEmpty(day) || !relativeDate)
			{
				string format;
				if (abbreviated)
				{
					format = dayOfTheWeek ? "ddd d MMM yy" : "d MMM yy";
				}
				else
				{
					format = dayOfTheWeek ? "dddd d MMMM yyyy" : "d MMMM yyyy";
				}

				day = dateTimeToFormat.ToString(format, CultureInfo.CurrentCulture);
				if (abbreviated || relativeDate)
				{
					int? pos;
					// try to lop off the current year
					string year;
					if (abbreviated)
					{
						pos = day.Length - 2;
						year = currentDateTime.ToString("yy", CultureInfo.CurrentCulture);
					}
					else
					{
						pos = day.Length - 4;
						year = currentDateTime.ToString("yyyy", CultureInfo.CurrentCulture);
					}
					if (day.Substring(pos.Value) == year) { day = day.Substring(0, pos.Value - 1); }
				}
			}
			return day;
		}
	}
}
