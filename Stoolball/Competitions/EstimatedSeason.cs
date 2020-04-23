using System;

namespace Stoolball.Competitions
{
	/// <summary>
	/// Estimates and sets the start and end times of a stoolball season based on a given <see cref="DateTimeOffset"/> (which defaults to now)
	/// </summary>
	public class EstimatedSeason : IEstimatedSeason
	{
		private readonly DateTimeOffset _dateToEstimateFrom;

		public EstimatedSeason(DateTimeOffset dateToEstimateFrom)
		{
			if (dateToEstimateFrom == null) { throw new ArgumentNullException(nameof(dateToEstimateFrom)); }
			_dateToEstimateFrom = dateToEstimateFrom;
			EstimateSeasonDates();
		}
		public EstimatedSeason() : this(DateTimeOffset.UtcNow) { }

		public DateTimeOffset StartDate { get; set; }
		public DateTimeOffset EndDate { get; set; }

		private void EstimateSeasonDates()
		{
			var summerSeasonStartMonth = 4;
			var winterSeasonStartMonth = 10;

			if (_dateToEstimateFrom.Month < summerSeasonStartMonth || _dateToEstimateFrom.Month >= winterSeasonStartMonth)
			{
				// Winter/indoor season
				StartDate = new DateTimeOffset(_dateToEstimateFrom.Month < summerSeasonStartMonth ? _dateToEstimateFrom.Year - 1 : _dateToEstimateFrom.Year, winterSeasonStartMonth, 1, 0, 0, 0, new TimeSpan());
				EndDate = new DateTimeOffset(_dateToEstimateFrom.Month < summerSeasonStartMonth ? _dateToEstimateFrom.Year : _dateToEstimateFrom.Year + 1, summerSeasonStartMonth - 1, 31, 23, 59, 59, new TimeSpan());
			}
			else
			{
				// Summer/outdoor season
				StartDate = new DateTimeOffset(_dateToEstimateFrom.Year, summerSeasonStartMonth, 1, 0, 0, 1, new TimeSpan());
				EndDate = new DateTimeOffset(_dateToEstimateFrom.Year, winterSeasonStartMonth - 1, 30, 23, 59, 59, new TimeSpan());
			}
		}
	}
}
