using System;

namespace Stoolball.Competitions
{
    public interface IEstimatedSeason
    {
        DateTimeOffset EndDate { get; set; }
        DateTimeOffset StartDate { get; set; }
    }
}