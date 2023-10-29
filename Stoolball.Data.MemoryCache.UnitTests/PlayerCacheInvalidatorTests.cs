using System;
using Moq;
using Stoolball.Data.Abstractions;
using Stoolball.Routing;
using Stoolball.Statistics;
using Xunit;

namespace Stoolball.Data.MemoryCache.UnitTests
{
    public class PlayerCacheInvalidatorTests
    {
        private readonly Mock<IReadThroughCache> _cache = new();
        private readonly Mock<IRouteNormaliser> _normaliser = new();

        [Theory]
        [InlineData("/players/example")]
        [InlineData("/players/example/")]
        [InlineData("/players/example/some-page")]
        public void Clears_cache_for_reading_players_by_normalised_PlayerRoute_and_MemberKey_and_for_player_summary_statistics(string route)
        {
            var player = new Player { PlayerRoute = route, MemberKey = Guid.NewGuid() };
            const string normalisedRoute = "/players/example";
            _normaliser.Setup(x => x.NormaliseRouteToEntity(player.PlayerRoute, "players")).Returns(normalisedRoute);
            var cacheClearer = new PlayerCacheInvalidator(_cache.Object, _normaliser.Object);

            cacheClearer.InvalidateCacheForPlayer(player);

            _cache.Verify(x => x.InvalidateCache(nameof(IPlayerDataSource) + nameof(IPlayerDataSource.ReadPlayerByRoute) + normalisedRoute), Times.Once);
            _cache.Verify(x => x.InvalidateCache(nameof(IPlayerDataSource) + nameof(IPlayerDataSource.ReadPlayerByMemberKey) + player.MemberKey), Times.Once);
            _cache.Verify(x => x.InvalidateCache(nameof(IPlayerSummaryStatisticsDataSource) + nameof(IPlayerSummaryStatisticsDataSource.ReadBattingStatistics) + normalisedRoute), Times.Once);
            _cache.Verify(x => x.InvalidateCache(nameof(IPlayerSummaryStatisticsDataSource) + nameof(IPlayerSummaryStatisticsDataSource.ReadBowlingStatistics) + normalisedRoute), Times.Once);
            _cache.Verify(x => x.InvalidateCache(nameof(IPlayerSummaryStatisticsDataSource) + nameof(IPlayerSummaryStatisticsDataSource.ReadFieldingStatistics) + normalisedRoute), Times.Once);
        }

        [Fact]
        public void Skips_clearing_ReadPlayerByMemberKey_cache_for_missing_MemberKey()
        {
            var player = new Player { PlayerRoute = "/players/example", MemberKey = null };
            var cacheClearer = new PlayerCacheInvalidator(_cache.Object, _normaliser.Object);

            cacheClearer.InvalidateCacheForPlayer(player);

            _cache.Verify(x => x.InvalidateCache(nameof(IPlayerDataSource) + nameof(IPlayerDataSource.ReadPlayerByMemberKey)), Times.Never);
        }

        [Fact]
        public void Throws_ArgumentNullException_for_missing_Player()
        {
            var cacheClearer = new PlayerCacheInvalidator(_cache.Object, _normaliser.Object);

#nullable disable
            Assert.Throws<ArgumentNullException>(() => cacheClearer.InvalidateCacheForPlayer(null));
#nullable enable
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public void Throws_ArgumentException_for_missing_PlayerRoute(string route)
        {
            var player = new Player { PlayerRoute = route, MemberKey = Guid.NewGuid() };
            var cacheClearer = new PlayerCacheInvalidator(_cache.Object, _normaliser.Object);

            Assert.Throws<ArgumentException>(() => cacheClearer.InvalidateCacheForPlayer(player));
        }
    }
}
