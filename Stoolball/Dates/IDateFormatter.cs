using System;

namespace Stoolball.Dates
{
	public interface IDateFormatter
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
		string FormatDate(DateTimeOffset dateTimeToFormat, DateTimeOffset currentDateTime, bool dayOfTheWeek = true, bool time = true, bool relativeDate = true, bool abbreviated = false);

		/// <summary>
		/// Get a date in the format "10am, Sunday 1 January 2000"
		/// </summary>
		/// <param name="dateTime">The date and time to display</param>
		/// <param name="dayOfTheWeek">Include Monday, Tuesday, Wednesday etc</param>
		/// <param name="time">Include the time of day</param>
		/// <param name="relativeDate">Refer to "this Sunday" rather than "Sunday 12 June"</param>
		/// <param name="abbreviated">Use Jan, Feb etc rather than January, February etc</param>
		/// <returns></returns>
		string FormatDate(DateTimeOffset dateTimeToFormat, bool dayOfTheWeek = true, bool time = true, bool relativeDate = true, bool abbreviated = false);

		/// <summary>
		/// Gets the time in the format 10am or 10.01am
		/// </summary>
		string FormatTime(DateTimeOffset dateTimeToFormat);
	}
}