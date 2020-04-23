using System;

namespace Stoolball.Dates
{
	public interface IDateFormatter
	{
		/// <summary>
		/// Get a date in the format "10am, Sunday 1 January 2000"
		/// </summary>
		/// <param name="dateTime">The date and time to display</param>
		/// <param name="dayOfTheWeek">Include Monday, Tuesday, Wednesday etc</param>
		/// <param name="relativeDate">Refer to "this Sunday" rather than "Sunday 12 June"</param>
		/// <param name="abbreviated">Use Jan, Feb etc rather than January, February etc</param>
		/// <returns></returns>
		string FormatDate(DateTimeOffset dateTimeToFormat, DateTimeOffset currentDateTime, bool dayOfTheWeek = true, bool relativeDate = true, bool abbreviated = false);

		/// <summary>
		/// Get a date in the format "10am, Sunday 1 January 2000"
		/// </summary>
		/// <param name="dateTime">The date and time to display</param>
		/// <param name="dayOfTheWeek">Include Monday, Tuesday, Wednesday etc</param>
		/// <param name="relativeDate">Refer to "this Sunday" rather than "Sunday 12 June"</param>
		/// <param name="abbreviated">Use Jan, Feb etc rather than January, February etc</param>
		/// <returns></returns>
		string FormatDate(DateTimeOffset dateTimeToFormat, bool dayOfTheWeek = true, bool relativeDate = true, bool abbreviated = false);
	}
}