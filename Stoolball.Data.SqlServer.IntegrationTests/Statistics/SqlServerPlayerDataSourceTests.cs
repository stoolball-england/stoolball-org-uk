﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Moq;
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
        public async Task Read_player_identities_returns_basic_fields()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var playerDataSource = new SqlServerPlayerDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var results = await playerDataSource.ReadPlayerIdentities(null).ConfigureAwait(false);

            foreach (var identity in _databaseFixture.PlayerWithMultipleIdentities.PlayerIdentities)
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
        public async Task Read_player_identities_returns_team()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var playerDataSource = new SqlServerPlayerDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var results = await playerDataSource.ReadPlayerIdentities(null).ConfigureAwait(false);

            foreach (var identity in _databaseFixture.PlayerIdentities)
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

            Assert.Equal(_databaseFixture.PlayerIdentities.Count, results.Count);
            foreach (var identity in _databaseFixture.PlayerIdentities)
            {
                Assert.NotNull(results.SingleOrDefault(x => x.PlayerIdentityId == identity.PlayerIdentityId));
            }
        }

        [Fact]
        public async Task Read_player_identities_supports_case_insensitive_filter_by_name()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            var playerDataSource = new SqlServerPlayerDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);
            var expected = _databaseFixture.PlayerWithMultipleIdentities.PlayerIdentities.First();

            var results = await playerDataSource.ReadPlayerIdentities(new PlayerIdentityFilter
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

            var results = await playerDataSource.ReadPlayerIdentities(new PlayerIdentityFilter { TeamIds = new List<Guid> { _databaseFixture.PlayerWithMultipleIdentities.PlayerIdentities[0].Team.TeamId.Value } }).ConfigureAwait(false);

            var expected = _databaseFixture.PlayerIdentities.Where(x => x.Team.TeamId == _databaseFixture.PlayerWithMultipleIdentities.PlayerIdentities[0].Team.TeamId.Value);
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

            var results = await playerDataSource.ReadPlayerIdentities(new PlayerIdentityFilter { PlayerIdentityIds = _databaseFixture.PlayerWithMultipleIdentities.PlayerIdentities.Select(x => x.PlayerIdentityId.Value).ToList() }).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.PlayerWithMultipleIdentities.PlayerIdentities.Count, results.Count);
            foreach (var identity in _databaseFixture.PlayerWithMultipleIdentities.PlayerIdentities)
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
            routeNormaliser.Setup(x => x.NormaliseRouteToEntity(_databaseFixture.PlayerWithMultipleIdentities.PlayerRoute, "players")).Returns(_databaseFixture.PlayerWithMultipleIdentities.PlayerRoute);
            var playerDataSource = new SqlServerPlayerDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await playerDataSource.ReadPlayerByRoute(_databaseFixture.PlayerWithMultipleIdentities.PlayerRoute).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.PlayerWithMultipleIdentities.PlayerId, result.PlayerId);
            Assert.Equal(_databaseFixture.PlayerWithMultipleIdentities.PlayerIdentities.Count, result.PlayerIdentities.Count);
            foreach (var identity in _databaseFixture.PlayerWithMultipleIdentities.PlayerIdentities)
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