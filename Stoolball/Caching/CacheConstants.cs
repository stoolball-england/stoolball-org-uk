using System;

namespace Stoolball.Caching
{
    public static class CacheConstants
    {
        public static TimeSpan StatisticsExpiration() => TimeSpan.FromMinutes(120);
        public const string MatchesPolicy = "matches";
        public const string TeamsPolicy = "teams";
        public const string MatchLocationsPolicy = "locations";
        public const string CompetitionsPolicy = "competitions";
        public const string MatchListingsCacheKeyPrefix = "ReadMatchListings";
        public const string TeamListingsCacheKeyPrefix = "ReadTeamListings";
        public const string MemberOverridePolicy = "MemberOverride";
        public const string CommentsPolicy = "comments";
        public const string MatchLocationsCacheKeyPrefix = "ReadMatchLocations";
        public const string CompetitionsPolicyCacheKeyPrefix = "ReadCompetitions";
    }
}
