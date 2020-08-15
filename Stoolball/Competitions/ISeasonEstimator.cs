using System;

namespace Stoolball.Competitions
{
    public interface ISeasonEstimator
    {
        (DateTimeOffset fromDate, DateTimeOffset untilDate) EstimateSeasonDates(DateTimeOffset dateToEstimateFrom);
    }
}