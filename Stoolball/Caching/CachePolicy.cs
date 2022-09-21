using System;

namespace Stoolball.Caching
{
    public static class CachePolicy
    {
        public static TimeSpan StatisticsExpiration() => TimeSpan.FromMinutes(120);
        public static TimeSpan CompetitionsExpiration() => TimeSpan.FromDays(1);
        public static TimeSpan MatchLocationsExpiration() => TimeSpan.FromDays(1);
        public static TimeSpan TeamsExpiration() => TimeSpan.FromDays(1);
        public static TimeSpan MatchesExpiration() => TimeSpan.FromDays(1);
        public static TimeSpan CommentsExpiration() => TimeSpan.FromDays(1);
    }
}
