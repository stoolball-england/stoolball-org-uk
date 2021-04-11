using System;
using System.Collections.Generic;
using Stoolball.Competitions;

namespace Stoolball.Matches
{
    public class MatchFilterFactory : IMatchFilterFactory
    {
        private readonly ISeasonEstimator _seasonEstimator;

        public MatchFilterFactory(ISeasonEstimator seasonEstimator)
        {
            _seasonEstimator = seasonEstimator ?? throw new ArgumentNullException(nameof(seasonEstimator));
        }

        public (MatchFilter filter, MatchSortOrder sortOrder) MatchesForTeams(List<Guid> teamIds)
        {
            return (new MatchFilter
            {
                TeamIds = teamIds ?? new List<Guid>(),
                FromDate = _seasonEstimator.EstimateSeasonDates(DateTimeOffset.UtcNow).fromDate
            }, MatchSortOrder.MatchDateEarliestFirst);
        }

        public (MatchFilter filter, MatchSortOrder sortOrder) MatchesForMatchLocation(Guid matchLocationId)
        {
            return (new MatchFilter
            {
                MatchLocationIds = new List<Guid> { matchLocationId },
                FromDate = _seasonEstimator.EstimateSeasonDates(DateTimeOffset.UtcNow).fromDate
            }, MatchSortOrder.MatchDateEarliestFirst);
        }

        public (MatchFilter filter, MatchSortOrder sortOrder) MatchesForSeason(Guid seasonId)
        {
            return (new MatchFilter
            {
                SeasonIds = new List<Guid> { seasonId }
            }, MatchSortOrder.MatchDateEarliestFirst);
        }
    }
}
