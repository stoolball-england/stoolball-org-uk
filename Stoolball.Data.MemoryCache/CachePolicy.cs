using System;

namespace Stoolball.Data.MemoryCache
{
    internal static class CachePolicy
    {
        internal static TimeSpan StatisticsExpiration() => TimeSpan.FromMinutes(120);
        internal static TimeSpan CompetitionsExpiration() => TimeSpan.FromDays(1);
        internal static TimeSpan MatchLocationsExpiration() => TimeSpan.FromDays(1);
        internal static TimeSpan TeamsExpiration() => TimeSpan.FromDays(1);
        internal static TimeSpan MatchesExpiration() => TimeSpan.FromDays(1);
        internal static TimeSpan CommentsExpiration() => TimeSpan.FromDays(1);
    }
}
