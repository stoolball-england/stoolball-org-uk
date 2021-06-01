using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using Stoolball.Data.SqlServer.IntegrationTests.Fixtures;
using Stoolball.Routing;
using Stoolball.Statistics;
using Xunit;

namespace Stoolball.Data.SqlServer.IntegrationTests.Statistics
{
    [Collection(IntegrationTestConstants.StatisticsDataSourceIntegrationTestCollection)]
    public class SqlServerPlayerDataSourceTests
    {
        private readonly SqlServerStatisticsDataSourceFixture _databaseFixture;

        public SqlServerPlayerDataSourceTests(SqlServerStatisticsDataSourceFixture databaseFixture)
        {
            _databaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));
        }

        [Fact]
        public async Task Read_players_returns_player_with_identities_and_teams()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var playerDataSource = new SqlServerPlayerDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var results = await playerDataSource.ReadPlayers(null).ConfigureAwait(false);

            foreach (var player in _databaseFixture.TestData.Players)
            {
                var result = results.SingleOrDefault(x => x.PlayerId == player.PlayerId);
                Assert.NotNull(result);
                Assert.Equal(player.PlayerRoute, result.PlayerRoute);

                foreach (var identity in player.PlayerIdentities)
                {
                    var resultIdentity = result.PlayerIdentities.SingleOrDefault(x => x.PlayerIdentityId == identity.PlayerIdentityId);
                    Assert.NotNull(resultIdentity);
                    Assert.Equal(identity.PlayerIdentityName, resultIdentity.PlayerIdentityName);
                    Assert.Equal(identity.FirstPlayed?.AccurateToTheMinute(), resultIdentity.FirstPlayed?.AccurateToTheMinute());
                    Assert.Equal(identity.LastPlayed?.AccurateToTheMinute(), resultIdentity.LastPlayed?.AccurateToTheMinute());
                    Assert.Equal(identity.Team.TeamName, resultIdentity.Team.TeamName);
                }
            }
        }


        [Fact]
        public async Task Read_players_supports_no_filter()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var playerDataSource = new SqlServerPlayerDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var results = await playerDataSource.ReadPlayers(null).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.TestData.Players.Count, results.Count);
            foreach (var player in _databaseFixture.TestData.Players)
            {
                Assert.NotNull(results.SingleOrDefault(x => x.PlayerId == player.PlayerId));
            }
        }

        [Fact]
        public async Task Read_players_supports_filter_by_player_id()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var playerDataSource = new SqlServerPlayerDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);
            var expectedIds = _databaseFixture.TestData.Players.Take(3).Select(x => x.PlayerId.Value).ToList();

            var results = await playerDataSource.ReadPlayers(new PlayerFilter { PlayerIds = expectedIds }).ConfigureAwait(false);

            Assert.Equal(expectedIds.Count, results.Count);
            foreach (var playerId in expectedIds)
            {
                Assert.NotNull(results.SingleOrDefault(x => x.PlayerId == playerId));
            }
        }

        [Fact]
        public async Task Read_player_identities_returns_basic_fields()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var playerDataSource = new SqlServerPlayerDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var results = await playerDataSource.ReadPlayerIdentities(null).ConfigureAwait(false);

            foreach (var identity in _databaseFixture.TestData.BowlerWithMultipleIdentities.PlayerIdentities)
            {
                var result = results.SingleOrDefault(x => x.PlayerIdentityId == identity.PlayerIdentityId);
                Assert.NotNull(result);

                Assert.Equal(identity.PlayerIdentityName, result.PlayerIdentityName);
                Assert.Equal(identity.TotalMatches, result.TotalMatches);
                Assert.Equal(identity.FirstPlayed.Value.AccurateToTheMinute(), result.FirstPlayed.Value.AccurateToTheMinute());
                Assert.Equal(identity.LastPlayed.Value.AccurateToTheMinute(), result.LastPlayed.Value.AccurateToTheMinute());
            }
        }

        [Fact]
        public async Task Read_player_identities_returns_player()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var playerDataSource = new SqlServerPlayerDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var results = await playerDataSource.ReadPlayerIdentities(null).ConfigureAwait(false);

            foreach (var identity in _databaseFixture.TestData.PlayerIdentities)
            {
                var result = results.SingleOrDefault(x => x.PlayerIdentityId == identity.PlayerIdentityId);
                Assert.NotNull(result);

                Assert.Equal(identity.Player.PlayerId, result.Player.PlayerId);
                Assert.Equal(identity.Player.PlayerRoute, result.Player.PlayerRoute);
            }
        }

        [Fact]
        public async Task Read_player_identities_returns_team()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var playerDataSource = new SqlServerPlayerDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var results = await playerDataSource.ReadPlayerIdentities(null).ConfigureAwait(false);

            foreach (var identity in _databaseFixture.TestData.PlayerIdentities)
            {
                var result = results.SingleOrDefault(x => x.PlayerIdentityId == identity.PlayerIdentityId);
                Assert.NotNull(result);

                Assert.Equal(identity.Team.TeamId, result.Team.TeamId);
                Assert.Equal(identity.Team.TeamName, result.Team.TeamName);
            }
        }

        [Fact]
        public async Task Read_player_identities_supports_no_filter()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var playerDataSource = new SqlServerPlayerDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var results = await playerDataSource.ReadPlayerIdentities(null).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.TestData.PlayerIdentities.Count, results.Count);
            foreach (var identity in _databaseFixture.TestData.PlayerIdentities)
            {
                Assert.NotNull(results.SingleOrDefault(x => x.PlayerIdentityId == identity.PlayerIdentityId));
            }
        }

        [Fact]
        public async Task Read_player_identities_supports_case_insensitive_filter_by_name()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var playerDataSource = new SqlServerPlayerDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);
            var expected = _databaseFixture.TestData.BowlerWithMultipleIdentities.PlayerIdentities.First();

            var results = await playerDataSource.ReadPlayerIdentities(new PlayerFilter
            {
                Query = expected.PlayerIdentityName.ToLower(CultureInfo.CurrentCulture).Substring(0, 5) + expected.PlayerIdentityName.ToUpperInvariant().Substring(5)
            }).ConfigureAwait(false);

            Assert.Single(results);
            Assert.Equal(expected.PlayerIdentityId, results[0].PlayerIdentityId);
        }

        [Fact]
        public async Task Read_player_identities_supports_filter_by_team_id()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var playerDataSource = new SqlServerPlayerDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var results = await playerDataSource.ReadPlayerIdentities(new PlayerFilter { TeamIds = new List<Guid> { _databaseFixture.TestData.BowlerWithMultipleIdentities.PlayerIdentities[0].Team.TeamId.Value } }).ConfigureAwait(false);

            var expected = _databaseFixture.TestData.PlayerIdentities.Where(x => x.Team.TeamId == _databaseFixture.TestData.BowlerWithMultipleIdentities.PlayerIdentities[0].Team.TeamId.Value);
            Assert.Equal(expected.Count(), results.Count);
            foreach (var identity in expected)
            {
                Assert.NotNull(results.SingleOrDefault(x => x.PlayerIdentityId == identity.PlayerIdentityId));
            }
        }

        [Fact]
        public async Task Read_player_identities_supports_filter_by_player_identity_id()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var playerDataSource = new SqlServerPlayerDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var results = await playerDataSource.ReadPlayerIdentities(new PlayerFilter { PlayerIdentityIds = _databaseFixture.TestData.BowlerWithMultipleIdentities.PlayerIdentities.Select(x => x.PlayerIdentityId.Value).ToList() }).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.TestData.BowlerWithMultipleIdentities.PlayerIdentities.Count, results.Count);
            foreach (var identity in _databaseFixture.TestData.BowlerWithMultipleIdentities.PlayerIdentities)
            {
                Assert.NotNull(results.SingleOrDefault(x => x.PlayerIdentityId == identity.PlayerIdentityId));
            }
        }

        [Fact]
        public async Task Read_player_identities_sorts_by_team_first_then_probability()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var playerDataSource = new SqlServerPlayerDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var results = await playerDataSource.ReadPlayerIdentities(null).ConfigureAwait(false);

            Guid? expectedTeam = null;
            var previousTeams = new List<Guid>();
            var previousProbability = int.MaxValue;
            foreach (var identity in results)
            {
                // The first time any team is seen, set a flag to say they must all be that team.
                // Record the teams already seen so that we can't switch back to a previous one.
                // Also reset the tracker that says probability must count down for the team.
                if (identity.Team.TeamId != expectedTeam && !previousTeams.Contains(identity.Team.TeamId.Value))
                {
                    expectedTeam = identity.Team.TeamId;
                    previousTeams.Add(expectedTeam.Value);
                    previousProbability = int.MaxValue;
                }
                Assert.Equal(expectedTeam, identity.Team.TeamId);
                Assert.True(identity.Probability <= previousProbability);
                previousProbability = identity.Probability.Value;
            }
            Assert.NotNull(expectedTeam);
            Assert.NotEqual(expectedTeam, previousTeams[0]);
        }

        [Fact]
        public async Task Read_player_by_route_returns_multiple_player_identities_with_teams()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            routeNormaliser.Setup(x => x.NormaliseRouteToEntity(_databaseFixture.TestData.BowlerWithMultipleIdentities.PlayerRoute, "players")).Returns(_databaseFixture.TestData.BowlerWithMultipleIdentities.PlayerRoute);
            var playerDataSource = new SqlServerPlayerDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await playerDataSource.ReadPlayerByRoute(_databaseFixture.TestData.BowlerWithMultipleIdentities.PlayerRoute).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.TestData.BowlerWithMultipleIdentities.PlayerId, result.PlayerId);
            Assert.Equal(_databaseFixture.TestData.BowlerWithMultipleIdentities.PlayerRoute, result.PlayerRoute);
            Assert.Equal(_databaseFixture.TestData.BowlerWithMultipleIdentities.PlayerIdentities.Count, result.PlayerIdentities.Count);
            foreach (var identity in _databaseFixture.TestData.BowlerWithMultipleIdentities.PlayerIdentities)
            {
                var resultIdentity = result.PlayerIdentities.SingleOrDefault(x => x.PlayerIdentityId == identity.PlayerIdentityId);
                Assert.NotNull(resultIdentity);

                Assert.Equal(identity.PlayerIdentityName, resultIdentity.PlayerIdentityName);
                Assert.Equal(identity.Team.TeamName, resultIdentity.Team.TeamName);
                Assert.Equal(identity.Team.TeamRoute, resultIdentity.Team.TeamRoute);
                Assert.Equal(identity.TotalMatches, resultIdentity.TotalMatches);
                Assert.Equal(identity.FirstPlayed.Value.AccurateToTheMinute(), resultIdentity.FirstPlayed.Value.AccurateToTheMinute());
                Assert.Equal(identity.LastPlayed.Value.AccurateToTheMinute(), resultIdentity.LastPlayed.Value.AccurateToTheMinute());
            }
        }
    }
}
