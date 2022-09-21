using System;

namespace Stoolball.Caching
{
    public static class CacheConstants
    {
        public static TimeSpan StatisticsExpiration() => TimeSpan.FromMinutes(120);
        public static TimeSpan CompetitionsExpiration() => TimeSpan.FromDays(1);
        public static TimeSpan MatchLocationsExpiration() => TimeSpan.FromDays(1);
        public static TimeSpan TeamsExpiration() => TimeSpan.FromDays(1);
        public const string MatchesPolicy = "matches";
        public const string MatchListingsCacheKeyPrefix = "ReadMatchListings";
        public static TimeSpan CommentsExpiration() => TimeSpan.FromDays(1);
    }
}
