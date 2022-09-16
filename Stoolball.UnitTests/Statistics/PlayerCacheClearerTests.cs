using System;
using System.Threading.Tasks;
using Moq;
using Stoolball.Caching;
using Stoolball.Statistics;
using Xunit;

namespace Stoolball.UnitTests.Statistics
{
    public class PlayerCacheClearerTests
    {
        private readonly Mock<IReadThroughCache> _cache = new();

        [Fact]
        public async Task Clears_cache_for_reading_players_by_PlayerRoute_and_MemberKey()
        {
            var player = new Player { PlayerRoute = "/players/example", MemberKey = Guid.NewGuid() };
            var cacheClearer = new PlayerCacheClearer(_cache.Object);

            await cacheClearer.ClearCacheFor(player);

            _cache.Verify(x => x.InvalidateCache(nameof(IPlayerDataSource.ReadPlayerByRoute) + player.PlayerRoute), Times.Once);
            _cache.Verify(x => x.InvalidateCache(nameof(IPlayerDataSource.ReadPlayerByMemberKey) + player.MemberKey), Times.Once);
        }

        [Fact]
        public async Task Clears_cache_for_missing_MemberKey()
        {
            var player = new Player { PlayerRoute = "/players/example", MemberKey = null };
            var cacheClearer = new PlayerCacheClearer(_cache.Object);

            await cacheClearer.ClearCacheFor(player);

            _cache.Verify(x => x.InvalidateCache(nameof(IPlayerDataSource.ReadPlayerByMemberKey)), Times.Once);
        }

        [Fact]
        public async Task Throws_ArgumentNullException_for_missing_Player()
        {
            var cacheClearer = new PlayerCacheClearer(_cache.Object);

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await cacheClearer.ClearCacheFor(null));
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public async Task Throws_ArgumentException_for_missing_PlayerRoute(string route)
        {
            var player = new Player { PlayerRoute = route, MemberKey = Guid.NewGuid() };
            var cacheClearer = new PlayerCacheClearer(_cache.Object);

            await Assert.ThrowsAsync<ArgumentException>(async () => await cacheClearer.ClearCacheFor(player));
        }
    }
}
