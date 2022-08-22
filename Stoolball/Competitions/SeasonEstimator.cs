using System;

namespace Stoolball.Competitions
{
    /// <summary>
    /// Estimates and sets the start and end times of a stoolball season based on a given <see cref="DateTimeOffset"/>
    /// </summary>
    public class SeasonEstimator : ISeasonEstimator
    {
        public (DateTimeOffset fromDate, DateTimeOffset untilDate) EstimateSeasonDates(DateTimeOffset dateToEstimateFrom)
        {
            var summerSeasonStartMonth = 4;
            var winterSeasonStartMonth = 10;

            if (dateToEstimateFrom.Month < summerSeasonStartMonth || dateToEstimateFrom.Month >= winterSeasonStartMonth)
            {
                // Winter/indoor season
                var fromDate = new DateTimeOffset(dateToEstimateFrom.Month < summerSeasonStartMonth ? dateToEstimateFrom.Year - 1 : dateToEstimateFrom.Year, winterSeasonStartMonth, 1, 0, 0, 0, new TimeSpan());
                var untilDate = new DateTimeOffset(dateToEstimateFrom.Month < summerSeasonStartMonth ? dateToEstimateFrom.Year : dateToEstimateFrom.Year + 1, summerSeasonStartMonth - 1, 31, 23, 59, 59, new TimeSpan());
                return (fromDate, untilDate);
            }
            else
            {
                // Summer/outdoor season
                var fromDate = new DateTimeOffset(dateToEstimateFrom.Year, summerSeasonStartMonth, 1, 0, 0, 0, new TimeSpan());
                var untilDate = new DateTimeOffset(dateToEstimateFrom.Year, winterSeasonStartMonth - 1, 30, 23, 59, 59, new TimeSpan());
                return (fromDate, untilDate);
            }
        }
    }
}
