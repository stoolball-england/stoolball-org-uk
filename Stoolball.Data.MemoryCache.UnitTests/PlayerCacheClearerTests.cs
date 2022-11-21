using System;
using Moq;
using Stoolball.Data.Abstractions;
using Stoolball.Statistics;
using Xunit;

namespace Stoolball.Data.MemoryCache.UnitTests
{
    public class PlayerCacheClearerTests
    {
        private readonly Mock<IReadThroughCache> _cache = new();

        [Fact]
        public void Clears_cache_for_reading_players_by_PlayerRoute_and_MemberKey_and_for_player_summary_statistics()
        {
            var player = new Player { PlayerRoute = "/players/example", MemberKey = Guid.NewGuid() };
            var cacheClearer = new PlayerCacheInvalidator(_cache.Object);

            cacheClearer.InvalidateCacheForPlayer(player);

            _cache.Verify(x => x.InvalidateCache(nameof(IPlayerDataSource) + nameof(IPlayerDataSource.ReadPlayerByRoute) + player.PlayerRoute), Times.Once);
            _cache.Verify(x => x.InvalidateCache(nameof(IPlayerDataSource) + nameof(IPlayerDataSource.ReadPlayerByMemberKey) + player.MemberKey), Times.Once);
            _cache.Verify(x => x.InvalidateCache(nameof(IPlayerSummaryStatisticsDataSource) + nameof(IPlayerSummaryStatisticsDataSource.ReadBattingStatistics) + player.PlayerRoute), Times.Once);
            _cache.Verify(x => x.InvalidateCache(nameof(IPlayerSummaryStatisticsDataSource) + nameof(IPlayerSummaryStatisticsDataSource.ReadBowlingStatistics) + player.PlayerRoute), Times.Once);
            _cache.Verify(x => x.InvalidateCache(nameof(IPlayerSummaryStatisticsDataSource) + nameof(IPlayerSummaryStatisticsDataSource.ReadFieldingStatistics) + player.PlayerRoute), Times.Once);
        }

        [Fact]
        public void Skips_clearing_ReadPlayerByMemberKey_cache_for_missing_MemberKey()
        {
            var player = new Player { PlayerRoute = "/players/example", MemberKey = null };
            var cacheClearer = new PlayerCacheInvalidator(_cache.Object);

            cacheClearer.InvalidateCacheForPlayer(player);

            _cache.Verify(x => x.InvalidateCache(nameof(IPlayerDataSource) + nameof(IPlayerDataSource.ReadPlayerByMemberKey)), Times.Never);
        }

        [Fact]
        public void Throws_ArgumentNullException_for_missing_Player()
        {
            var cacheClearer = new PlayerCacheInvalidator(_cache.Object);

            Assert.Throws<ArgumentNullException>(() => cacheClearer.InvalidateCacheForPlayer(null));
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void Throws_ArgumentException_for_missing_PlayerRoute(string route)
        {
            var player = new Player { PlayerRoute = route, MemberKey = Guid.NewGuid() };
            var cacheClearer = new PlayerCacheInvalidator(_cache.Object);

            Assert.Throws<ArgumentException>(() => cacheClearer.InvalidateCacheForPlayer(player));
        }
    }
}
