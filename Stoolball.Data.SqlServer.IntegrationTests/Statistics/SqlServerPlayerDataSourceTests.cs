﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using Stoolball.Data.SqlServer.IntegrationTests.Fixtures;
using Stoolball.Matches;
using Stoolball.Routing;
using Stoolball.Statistics;
using Stoolball.Testing;
using Xunit;

namespace Stoolball.Data.SqlServer.IntegrationTests.Statistics
{
    [Collection(IntegrationTestConstants.TestDataIntegrationTestCollection)]
    public class SqlServerPlayerDataSourceTests
    {
        private readonly IDatabaseConnectionFactory _connectionFactory;
        private readonly TestData _testData;
        private readonly Mock<IStatisticsQueryBuilder> _statisticsQueryBuilder = new();
        private readonly Mock<IRouteNormaliser> _routeNormaliser = new();

        public SqlServerPlayerDataSourceTests(SqlServerTestDataFixture databaseFixture)
        {
            _connectionFactory = databaseFixture?.ConnectionFactory ?? throw new ArgumentException($"{nameof(databaseFixture)}.{nameof(databaseFixture.ConnectionFactory)}cannot be null", nameof(databaseFixture));
            _testData = databaseFixture?.TestData ?? throw new ArgumentException($"{nameof(databaseFixture)}.{nameof(databaseFixture.TestData)} cannot be null", nameof(databaseFixture));
        }

        [Fact]
        public async Task Read_players_returns_player_with_identities_and_teams()
        {
            var playerDataSource = new SqlServerPlayerDataSource(_connectionFactory, _routeNormaliser.Object, _statisticsQueryBuilder.Object);
            var matchFinder = new MatchFinder();

            var results = await playerDataSource.ReadPlayers(null);

            foreach (var player in _testData.PlayersWhoHavePlayedAtLeastOneMatch())
            {
                var result = results.SingleOrDefault(x => x.PlayerId == player.PlayerId);
                Assert.NotNull(result);
                Assert.Equal(player.PlayerRoute, result!.PlayerRoute);

                foreach (var identity in player.PlayerIdentities)
                {
                    var resultIdentity = result.PlayerIdentities.SingleOrDefault(x => x.PlayerIdentityId == identity.PlayerIdentityId);
                    Assert.NotNull(resultIdentity);
                    Assert.NotNull(resultIdentity!.Team);
                    Assert.Equal(identity.PlayerIdentityName, resultIdentity!.PlayerIdentityName);
                    Assert.Equal(identity.FirstPlayed?.AccurateToTheMinute(), resultIdentity.FirstPlayed?.AccurateToTheMinute());
                    Assert.Equal(identity.LastPlayed?.AccurateToTheMinute(), resultIdentity.LastPlayed?.AccurateToTheMinute());
                    Assert.Equal(matchFinder.MatchesPlayedByPlayerIdentity(_testData.Matches, identity!.PlayerIdentityId!.Value).Count(), resultIdentity.TotalMatches);
                    Assert.Equal(identity.Team?.TeamId, resultIdentity.Team!.TeamId);
                    Assert.Equal(identity.Team?.TeamName, resultIdentity.Team.TeamName);
                }
            }
        }


        [Fact]
        public async Task Read_players_supports_no_filter()
        {
            var playerDataSource = new SqlServerPlayerDataSource(_connectionFactory, _routeNormaliser.Object, _statisticsQueryBuilder.Object);

            var results = await playerDataSource.ReadPlayers(null);


            var wtf = _testData.Players.Where(p => !p.PlayerIdentities.Any(pi => pi.FirstPlayed is not null));


            var expected = _testData.PlayersWhoHavePlayedAtLeastOneMatch().ToList();
            Assert.Equal(expected.Count, results.Count);
            foreach (var player in expected)
            {
                Assert.NotNull(results.SingleOrDefault(x => x.PlayerId == player.PlayerId));
            }
        }

        [Fact]
        public async Task Read_players_supports_filter_by_player_id()
        {
            var playerDataSource = new SqlServerPlayerDataSource(_connectionFactory, _routeNormaliser.Object, _statisticsQueryBuilder.Object);
            var playerWithMultipleIdentities = _testData.PlayersWithMultipleIdentities.First();
            var expectedPlayers = _testData.Players.Where(x => x.PlayerId != playerWithMultipleIdentities.PlayerId).Take(3).ToList();
            expectedPlayers.Add(playerWithMultipleIdentities);

            var results = await playerDataSource.ReadPlayers(new PlayerFilter { PlayerIds = expectedPlayers.Select(x => x.PlayerId!.Value).ToList() });

            Assert.Equal(expectedPlayers.Count, results.Count);
            foreach (var player in expectedPlayers)
            {
                var result = results.SingleOrDefault(x => x.PlayerId == player.PlayerId);
                Assert.NotNull(result);
                Assert.Equal(player.PlayerIdentities.Count, result!.PlayerIdentities.Count);
            }
        }

        [Fact]
        public async Task Read_players_supports_case_insensitive_filter_by_name()
        {
            var playerDataSource = new SqlServerPlayerDataSource(_connectionFactory, _routeNormaliser.Object, _statisticsQueryBuilder.Object);
            var expected = _testData.BowlerWithMultipleIdentities!.PlayerIdentities.First();

            var results = await playerDataSource.ReadPlayers(new PlayerFilter
            {
                Query = expected.PlayerIdentityName?.ToLower(CultureInfo.CurrentCulture).Substring(0, 5) + expected.PlayerIdentityName?.ToUpperInvariant().Substring(5)
            });

            Assert.Single(results);
            // When filtering by an aspect of the identity, don't return non-matching identities even for the same player
            Assert.Single(results[0].PlayerIdentities);
            Assert.Equal(expected.PlayerIdentityId, results[0].PlayerIdentities[0].PlayerIdentityId);
        }

        [Fact]
        public async Task Read_players_supports_filter_by_club_id()
        {
            var playerDataSource = new SqlServerPlayerDataSource(_connectionFactory, _routeNormaliser.Object, _statisticsQueryBuilder.Object);
            var clubIds = _testData.PlayerIdentities.Where(x => x.Team?.Club?.ClubId != null).Take(2).Select(x => x.Team.Club.ClubId!.Value).ToList();
            Assert.NotEmpty(clubIds);

            var results = await playerDataSource.ReadPlayers(new PlayerFilter { ClubIds = clubIds });

            var identitiesInSelectedClubs = _testData.PlayerIdentities.Where(x => x.Team?.Club?.ClubId != null && clubIds.Contains(x.Team.Club.ClubId.Value));
            var playerIdentityIdsInSelectedClubs = identitiesInSelectedClubs.Select(x => x.PlayerIdentityId);
            var expectedPlayers = identitiesInSelectedClubs.Select(x => x.Player).Distinct(new PlayerEqualityComparer());
            Assert.Equal(expectedPlayers.Count(), results.Count);
            foreach (var player in expectedPlayers)
            {
                var result = results.SingleOrDefault(x => x.PlayerId == player.PlayerId);
                Assert.NotNull(result);

                // When filtering by an aspect of the identity, don't return non-matching identities even for the same player
                Assert.DoesNotContain(result!.PlayerIdentities, x => !playerIdentityIdsInSelectedClubs.Contains(x.PlayerIdentityId));
            }
        }

        [Fact]
        public async Task Read_players_supports_filter_by_team_id()
        {
            var playerDataSource = new SqlServerPlayerDataSource(_connectionFactory, _routeNormaliser.Object, _statisticsQueryBuilder.Object);
            var expectedTeamId = _testData.BowlerWithMultipleIdentities!.PlayerIdentities[0].Team!.TeamId!.Value;

            var results = await playerDataSource.ReadPlayers(new PlayerFilter { TeamIds = new List<Guid> { expectedTeamId } });

            var expected = _testData.Players.Where(x => x.PlayerIdentities.Any(pi => pi.Team?.TeamId == expectedTeamId));
            Assert.Equal(expected.Count(), results.Count);
            foreach (var player in expected)
            {
                var result = results.SingleOrDefault(x => x.PlayerId == player.PlayerId);
                Assert.NotNull(result);

                // When filtering by an aspect of the identity, don't return non-matching identities even for the same player
                Assert.DoesNotContain(result!.PlayerIdentities, x => x.Team?.TeamId != expectedTeamId);
            }
        }

        [Fact]
        public async Task Read_players_supports_filter_by_match_location_id()
        {
            var matchLocationId = _testData.MatchInThePastWithFullDetails!.MatchLocation!.MatchLocationId!.Value;
            var matches = _testData.Matches.Where(x => x.MatchLocation?.MatchLocationId == matchLocationId);
            var playerFilter = new PlayerFilter { MatchLocationIds = new List<Guid> { matchLocationId } };

            await Read_players_supports_filter_by_involvement_in_a_set_of_matches(matches, playerFilter);
        }

        [Fact]
        public async Task Read_players_supports_filter_by_competition_id()
        {
            var competitionId = _testData.MatchInThePastWithFullDetails!.Season!.Competition!.CompetitionId!.Value;
            var matches = _testData.Matches.Where(x => x.Season?.Competition?.CompetitionId == competitionId);
            var playerFilter = new PlayerFilter { CompetitionIds = new List<Guid> { competitionId } };

            await Read_players_supports_filter_by_involvement_in_a_set_of_matches(matches, playerFilter);
        }

        [Fact]
        public async Task Read_players_supports_filter_by_season_id()
        {
            var seasonId = _testData.MatchInThePastWithFullDetails!.Season!.SeasonId!.Value;
            var matches = _testData.Matches.Where(x => x.Season?.SeasonId == seasonId);
            var playerFilter = new PlayerFilter { SeasonIds = new List<Guid> { seasonId } };

            await Read_players_supports_filter_by_involvement_in_a_set_of_matches(matches, playerFilter);
        }

        private async Task Read_players_supports_filter_by_involvement_in_a_set_of_matches(IEnumerable<Stoolball.Matches.Match> matches, PlayerFilter playerFilter)
        {
            var playerDataSource = new SqlServerPlayerDataSource(_connectionFactory, _routeNormaliser.Object, _statisticsQueryBuilder.Object);
            var playerIdentities = new List<PlayerIdentity>();
            var playerIdentityFinder = new PlayerIdentityFinder();
            var matchFinder = new MatchFinder();
            foreach (var match in matches)
            {
                playerIdentities.AddRange(playerIdentityFinder.PlayerIdentitiesInMatch(match));
            }
            var players = playerIdentities.Select(x => x.Player).Distinct(new PlayerEqualityComparer()).ToList();
            var playerIdentityIds = playerIdentities.Select(x => x.PlayerIdentityId).Distinct().ToList();

            var results = await playerDataSource.ReadPlayers(playerFilter);

            Assert.Equal(players.Count, results.Count);
            foreach (var player in players)
            {
                var result = results.SingleOrDefault(x => x.PlayerId == player?.PlayerId);
                Assert.NotNull(result);

                // When filtering by an aspect of the identity, don't return non-matching identities even for the same player
                Assert.DoesNotContain(result!.PlayerIdentities, x => !playerIdentityIds.Contains(x.PlayerIdentityId));

                // Total matches must be filtered by the same criteria
                foreach (var identity in result.PlayerIdentities)
                {
                    Assert.Equal(matchFinder.MatchesPlayedByPlayerIdentity(matches, identity.PlayerIdentityId!.Value).Count(), identity.TotalMatches);
                }
            }
        }

        [Fact]
        public async Task Read_players_supports_filter_by_player_identity_id()
        {
            var playerDataSource = new SqlServerPlayerDataSource(_connectionFactory, _routeNormaliser.Object, _statisticsQueryBuilder.Object);
            var player = _testData.BowlerWithMultipleIdentities!;

            var results = await playerDataSource.ReadPlayers(new PlayerFilter { PlayerIdentityIds = new List<Guid> { player.PlayerIdentities.First().PlayerIdentityId!.Value } });

            Assert.Single(results);
            Assert.Equal(player.PlayerId, results[0].PlayerId);
            // When filtering by an aspect of the identity, don't return non-matching identities even for the same player
            Assert.Single(results[0].PlayerIdentities);
        }

        [Fact]
        public async Task Read_players_supports_exclude_by_player_identity_id()
        {
            var players = _testData.PlayersWhoHavePlayedAtLeastOneMatch().ToList();
            var playerDataSource = new SqlServerPlayerDataSource(_connectionFactory, _routeNormaliser.Object, _statisticsQueryBuilder.Object);
            var playerWithOneIdentity = players.First(x => x.PlayerIdentities.Count == 1);
            var playerWithOtherIdentities = _testData.PlayersWithMultipleIdentities.First();
            var identityToExcludeOfSeveral = playerWithOtherIdentities.PlayerIdentities.First().PlayerIdentityId!.Value;

            var results = await playerDataSource.ReadPlayers(
                new PlayerFilter
                {
                    ExcludePlayerIdentityIds = new List<Guid> {
                        playerWithOneIdentity.PlayerIdentities.First().PlayerIdentityId!.Value,
                        identityToExcludeOfSeveral
                    }
                });

            Assert.Equal(players.Count - 1, results.Count);
            foreach (var player in players)
            {
                if (player.PlayerId == playerWithOneIdentity.PlayerId)
                {
                    Assert.Null(results.FirstOrDefault(x => x.PlayerId == player.PlayerId));
                }
                else if (player.PlayerId == playerWithOtherIdentities.PlayerId)
                {
                    var resultForThisPlayer = results.FirstOrDefault(x => x.PlayerId == player.PlayerId);
                    Assert.NotNull(resultForThisPlayer);

                    // The identity should be filtered but not the player
                    Assert.Equal(playerWithOtherIdentities.PlayerIdentities.Count - 1, resultForThisPlayer!.PlayerIdentities.Count);
                    Assert.DoesNotContain(resultForThisPlayer!.PlayerIdentities, x => x.PlayerIdentityId == identityToExcludeOfSeveral);
                }
                else
                {
                    Assert.NotNull(results.FirstOrDefault(x => x.PlayerId == player.PlayerId));
                }
            }

        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Read_players_supports_include_players_linked_to_member(bool expectPlayer)
        {
            var players = _testData.PlayersWhoHavePlayedAtLeastOneMatch().ToList();
            var playerDataSource = new SqlServerPlayerDataSource(_connectionFactory, _routeNormaliser.Object, _statisticsQueryBuilder.Object);
            var playersToExclude = players.Where(x => x.MemberKey.HasValue).Select(x => x.PlayerId).ToList();

            var results = await playerDataSource.ReadPlayers(new PlayerFilter { IncludePlayersAndIdentitiesLinkedToAMember = expectPlayer });

            if (expectPlayer)
            {
                Assert.Equal(players.Count, results.Count);
            }
            else
            {
                Assert.Equal(players.Count - playersToExclude.Count, results.Count);
            }

            foreach (var player in players)
            {
                if (expectPlayer || !playersToExclude.Contains(player.PlayerId))
                {
                    Assert.NotNull(results.SingleOrDefault(x => x.PlayerId == player.PlayerId));
                }
                else
                {
                    Assert.Null(results.SingleOrDefault(x => x.PlayerId == player.PlayerId));
                }
            }
        }

        [Fact]
        public async Task Read_player_identities_returns_basic_fields()
        {
            var playerDataSource = new SqlServerPlayerDataSource(_connectionFactory, _routeNormaliser.Object, _statisticsQueryBuilder.Object);

            var results = await playerDataSource.ReadPlayerIdentities(null);

            foreach (var identity in _testData.BowlerWithMultipleIdentities!.PlayerIdentities)
            {
                var result = results.SingleOrDefault(x => x.PlayerIdentityId == identity.PlayerIdentityId);
                Assert.NotNull(result);

                Assert.Equal(identity.PlayerIdentityName, result!.PlayerIdentityName);
                Assert.Equal(identity.RouteSegment, result!.RouteSegment);
                Assert.Equal(identity.TotalMatches, result.TotalMatches);
                Assert.Equal(identity.FirstPlayed!.Value.AccurateToTheMinute(), result.FirstPlayed!.Value.AccurateToTheMinute());
                Assert.Equal(identity.LastPlayed!.Value.AccurateToTheMinute(), result.LastPlayed!.Value.AccurateToTheMinute());
            }
        }

        [Fact]
        public async Task Read_player_identities_returns_player()
        {
            var playerDataSource = new SqlServerPlayerDataSource(_connectionFactory, _routeNormaliser.Object, _statisticsQueryBuilder.Object);

            var results = await playerDataSource.ReadPlayerIdentities(null);

            foreach (var identity in _testData.PlayerIdentitiesWhoHavePlayedAtLeastOneMatch())
            {
                var identityFromResults = results.SingleOrDefault(x => x.PlayerIdentityId == identity.PlayerIdentityId);
                Assert.NotNull(identityFromResults?.Player);

                Assert.Equal(identity.Player!.PlayerId, identityFromResults!.Player!.PlayerId);
                Assert.Equal(identity.Player.PlayerRoute, identityFromResults.Player.PlayerRoute);

                var allIdentitiesMatchingFilterForThisPlayer = results.Where(x => x.Player?.PlayerId == identity.Player.PlayerId).ToList();
                Assert.Equal(allIdentitiesMatchingFilterForThisPlayer.Count, identityFromResults.Player.PlayerIdentities.Count);
                foreach (var identityForPlayer in allIdentitiesMatchingFilterForThisPlayer)
                {
                    var resultIdentityForPlayer = identityFromResults.Player.PlayerIdentities.SingleOrDefault(x => x.PlayerIdentityId == identityForPlayer.PlayerIdentityId);
                    Assert.NotNull(resultIdentityForPlayer);
                }
            }
        }

        [Fact]
        public async Task Read_player_identities_returns_team()
        {
            var playerDataSource = new SqlServerPlayerDataSource(_connectionFactory, _routeNormaliser.Object, _statisticsQueryBuilder.Object);

            var results = await playerDataSource.ReadPlayerIdentities(null);

            foreach (var identity in _testData.PlayerIdentitiesWhoHavePlayedAtLeastOneMatch())
            {
                var result = results.SingleOrDefault(x => x.PlayerIdentityId == identity.PlayerIdentityId);
                Assert.NotNull(result);

                Assert.Equal(identity.Team?.TeamId, result!.Team?.TeamId);
                Assert.Equal(identity.Team?.TeamName, result.Team?.TeamName);
            }
        }

        [Fact]
        public async Task Read_player_identities_supports_no_filter()
        {
            var playerDataSource = new SqlServerPlayerDataSource(_connectionFactory, _routeNormaliser.Object, _statisticsQueryBuilder.Object);

            var results = await playerDataSource.ReadPlayerIdentities(null);

            var expected = _testData.PlayerIdentitiesWhoHavePlayedAtLeastOneMatch().ToList();
            Assert.Equal(expected.Count, results.Count);
            foreach (var identity in expected)
            {
                Assert.NotNull(results.SingleOrDefault(x => x.PlayerIdentityId == identity.PlayerIdentityId));
            }
        }

        [Fact]
        public async Task Read_player_identities_supports_filter_by_player_id()
        {
            var playerDataSource = new SqlServerPlayerDataSource(_connectionFactory, _routeNormaliser.Object, _statisticsQueryBuilder.Object);
            var expectedIds = _testData.Players.Where(x => x.PlayerIdentities.Count == 1).Take(3).Select(x => x.PlayerId!.Value).ToList();

            var results = await playerDataSource.ReadPlayerIdentities(new PlayerFilter { PlayerIds = expectedIds });

            Assert.Equal(expectedIds.Count, results.Count);
            foreach (var playerId in expectedIds)
            {
                Assert.NotNull(results.SingleOrDefault(x => x.Player?.PlayerId == playerId));
            }
        }

        [Fact]
        public async Task Read_player_identities_supports_case_insensitive_filter_by_name()
        {
            var playerDataSource = new SqlServerPlayerDataSource(_connectionFactory, _routeNormaliser.Object, _statisticsQueryBuilder.Object);
            var expected = _testData.BowlerWithMultipleIdentities!.PlayerIdentities.First();

            var results = await playerDataSource.ReadPlayerIdentities(new PlayerFilter
            {
                Query = expected.PlayerIdentityName!.ToLower(CultureInfo.CurrentCulture).Substring(0, 5) + expected.PlayerIdentityName.ToUpperInvariant().Substring(5)
            });

            Assert.Single(results);
            Assert.Equal(expected.PlayerIdentityId, results[0].PlayerIdentityId);
        }

        [Fact]
        public async Task Read_player_identities_supports_filter_by_club_id()
        {
            var playerDataSource = new SqlServerPlayerDataSource(_connectionFactory, _routeNormaliser.Object, _statisticsQueryBuilder.Object);
            var clubIds = _testData.PlayerIdentities.Where(x => x.Team?.Club?.ClubId != null).Take(2).Select(x => x.Team!.Club!.ClubId!.Value).ToList();
            Assert.NotEmpty(clubIds);

            var results = await playerDataSource.ReadPlayerIdentities(new PlayerFilter { ClubIds = clubIds });

            var expected = _testData.PlayerIdentities.Where(x => x.Team?.Club?.ClubId != null && clubIds.Contains(x.Team.Club.ClubId.Value));
            Assert.Equal(expected.Count(), results.Count);
            foreach (var identity in expected)
            {
                Assert.NotNull(results.SingleOrDefault(x => x.PlayerIdentityId == identity.PlayerIdentityId));
            }
        }

        [Fact]
        public async Task Read_player_identities_supports_filter_by_team_id()
        {
            var playerDataSource = new SqlServerPlayerDataSource(_connectionFactory, _routeNormaliser.Object, _statisticsQueryBuilder.Object);
            var expectedTeamId = _testData.BowlerWithMultipleIdentities!.PlayerIdentities[0].Team!.TeamId!.Value;

            var results = await playerDataSource.ReadPlayerIdentities(new PlayerFilter { TeamIds = new List<Guid> { expectedTeamId } });

            var expected = _testData.PlayerIdentities.Where(x => x.Team!.TeamId == expectedTeamId);
            Assert.Equal(expected.Count(), results.Count);
            foreach (var identity in expected)
            {
                Assert.NotNull(results.SingleOrDefault(x => x.PlayerIdentityId == identity.PlayerIdentityId));
            }
        }

        [Fact]
        public async Task Read_player_identities_supports_filter_by_match_location_id()
        {
            var expectedMatchLocationId = _testData.MatchInThePastWithFullDetails!.MatchLocation!.MatchLocationId!.Value;
            var matches = _testData.Matches.Where(x => x.MatchLocation?.MatchLocationId == expectedMatchLocationId);
            var playerFilter = new PlayerFilter { MatchLocationIds = new List<Guid> { expectedMatchLocationId } };

            await Read_player_identities_supports_filter_by_involvement_in_a_set_of_matches(matches, playerFilter);
        }

        [Fact]
        public async Task Read_player_identities_supports_filter_by_competition_id()
        {
            var expectedCompetitionId = _testData.MatchInThePastWithFullDetails!.Season!.Competition!.CompetitionId!.Value;
            var matches = _testData.Matches.Where(x => x.Season?.Competition?.CompetitionId == expectedCompetitionId);
            var playerFilter = new PlayerFilter { CompetitionIds = new List<Guid> { expectedCompetitionId } };

            await Read_player_identities_supports_filter_by_involvement_in_a_set_of_matches(matches, playerFilter);
        }

        [Fact]
        public async Task Read_player_identities_supports_filter_by_season_id()
        {
            var expectedSeasonId = _testData.MatchInThePastWithFullDetails!.Season!.SeasonId!.Value;
            var matches = _testData.Matches.Where(x => x.Season?.SeasonId == expectedSeasonId);
            var playerFilter = new PlayerFilter { SeasonIds = new List<Guid> { expectedSeasonId } };

            await Read_player_identities_supports_filter_by_involvement_in_a_set_of_matches(matches, playerFilter);
        }

        private async Task Read_player_identities_supports_filter_by_involvement_in_a_set_of_matches(IEnumerable<Stoolball.Matches.Match> matches, PlayerFilter playerFilter)
        {
            var playerDataSource = new SqlServerPlayerDataSource(_connectionFactory, _routeNormaliser.Object, _statisticsQueryBuilder.Object);
            var playerIdentities = new List<PlayerIdentity>();
            var playerIdentityFinder = new PlayerIdentityFinder();
            foreach (var match in matches)
            {
                playerIdentities.AddRange(playerIdentityFinder.PlayerIdentitiesInMatch(match).Where(x => !playerIdentities.Select(pi => pi.PlayerIdentityId).Contains(x.PlayerIdentityId)));
            }

            var results = await playerDataSource.ReadPlayerIdentities(playerFilter);

            Assert.Equal(playerIdentities.Count, results.Count);
            foreach (var identity in playerIdentities)
            {
                Assert.NotNull(results.SingleOrDefault(x => x.PlayerIdentityId == identity.PlayerIdentityId));
            }
        }

        [Fact]
        public async Task Read_player_identities_supports_filter_by_player_identity_id()
        {
            var playerDataSource = new SqlServerPlayerDataSource(_connectionFactory, _routeNormaliser.Object, _statisticsQueryBuilder.Object);
            var identities = _testData.PlayersWithMultipleIdentities.First().PlayerIdentities;

            var results = await playerDataSource.ReadPlayerIdentities(new PlayerFilter { PlayerIdentityIds = identities.Select(x => x.PlayerIdentityId!.Value).ToList() });

            Assert.Equal(identities.Count, results.Count);
            foreach (var identity in identities)
            {
                Assert.NotNull(results.SingleOrDefault(x => x.PlayerIdentityId == identity.PlayerIdentityId));
            }
        }

        [Fact]
        public async Task Read_player_identities_supports_exclude_by_player_identity_id()
        {
            var playerDataSource = new SqlServerPlayerDataSource(_connectionFactory, _routeNormaliser.Object, _statisticsQueryBuilder.Object);
            var identityToExclude = _testData.BowlerWithMultipleIdentities!.PlayerIdentities[0].PlayerIdentityId!.Value;

            var results = await playerDataSource.ReadPlayerIdentities(new PlayerFilter { ExcludePlayerIdentityIds = new List<Guid>() { identityToExclude } });

            var identities = _testData.PlayerIdentitiesWhoHavePlayedAtLeastOneMatch().ToList();
            Assert.Equal(identities.Count - 1, results.Count);
            foreach (var identity in identities)
            {
                if (identity.PlayerIdentityId == identityToExclude)
                {
                    Assert.Null(results.SingleOrDefault(x => x.PlayerIdentityId == identity.PlayerIdentityId));
                }
                else
                {
                    Assert.NotNull(results.SingleOrDefault(x => x.PlayerIdentityId == identity.PlayerIdentityId));
                }
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Read_player_identities_supports_include_players_linked_to_member(bool expectPlayer)
        {
            var playerDataSource = new SqlServerPlayerDataSource(_connectionFactory, _routeNormaliser.Object, _statisticsQueryBuilder.Object);
            var playersToExclude = _testData.PlayersWhoHavePlayedAtLeastOneMatch().Where(x => x.MemberKey.HasValue);
            var identitiesToExclude = playersToExclude.SelectMany(x => x.PlayerIdentities).Select(x => x.PlayerIdentityId).Distinct().ToList();

            var results = await playerDataSource.ReadPlayerIdentities(new PlayerFilter { IncludePlayersAndIdentitiesLinkedToAMember = expectPlayer });

            var identities = _testData.PlayerIdentitiesWhoHavePlayedAtLeastOneMatch().ToList();
            if (expectPlayer)
            {
                Assert.Equal(identities.Count, results.Count);
            }
            else
            {
                Assert.Equal(identities.Count - identitiesToExclude.Count, results.Count);
            }

            foreach (var identity in identities)
            {
                if (expectPlayer || !identitiesToExclude.Contains(identity.PlayerIdentityId))
                {
                    Assert.NotNull(results.SingleOrDefault(x => x.PlayerIdentityId == identity.PlayerIdentityId));
                }
                else
                {
                    Assert.Null(results.SingleOrDefault(x => x.PlayerIdentityId == identity.PlayerIdentityId));
                }
            }
        }

        [Fact]
        public async Task Read_player_identities_supports_include_players_with_multiple_identities()
        {
            var playerDataSource = new SqlServerPlayerDataSource(_connectionFactory, _routeNormaliser.Object, _statisticsQueryBuilder.Object);
            var playersToExclude = _testData.PlayersWithMultipleIdentities;
            var identitiesToExclude = playersToExclude.SelectMany(x => x.PlayerIdentities).Select(x => x.PlayerIdentityId).Distinct().ToList();
            var expectedIdentities = _testData.PlayerIdentitiesWhoHavePlayedAtLeastOneMatch().Where(x => !identitiesToExclude.Contains(x.PlayerIdentityId)).Select(id => id.PlayerIdentityId).ToList();

            var results = await playerDataSource.ReadPlayerIdentities(new PlayerFilter { IncludePlayersAndIdentitiesWithMultipleIdentities = false });

            Assert.Equal(expectedIdentities.Count, results.Count);

            foreach (var identity in expectedIdentities)
            {
                Assert.NotNull(results.SingleOrDefault(x => x.PlayerIdentityId == identity));
            }
            foreach (var identity in identitiesToExclude)
            {
                Assert.Null(results.SingleOrDefault(x => x.PlayerIdentityId == identity));
            }
        }

        [Fact]
        public async Task Read_player_identities_sorts_by_team_first_then_probability()
        {
            var playerDataSource = new SqlServerPlayerDataSource(_connectionFactory, _routeNormaliser.Object, _statisticsQueryBuilder.Object);

            var results = await playerDataSource.ReadPlayerIdentities(null);

            Guid? expectedTeam = null;
            var previousTeams = new List<Guid>();
            int? previousProbability = int.MaxValue;
            foreach (var identity in results)
            {
                // The first time any team is seen, set a flag to say they must all be that team.
                // Record the teams already seen so that we can't switch back to a previous one.
                // Also reset the tracker that says probability must count down for the team.
                if (identity.Team.TeamId != expectedTeam && !previousTeams.Contains(identity.Team.TeamId!.Value))
                {
                    expectedTeam = identity.Team.TeamId;
                    previousTeams.Add(expectedTeam.Value);
                    previousProbability = int.MaxValue;
                }
                Assert.Equal(expectedTeam, identity.Team.TeamId);
                Assert.True(identity.Probability <= previousProbability);
                previousProbability = identity.Probability;
            }
            Assert.NotNull(expectedTeam);
            Assert.NotEqual(expectedTeam, previousTeams[0]);
        }

        [Fact]
        public async Task Read_player_by_route_supports_no_filter_returns_multiple_player_identities_with_teams()
        {
            _routeNormaliser.Setup(x => x.NormaliseRouteToEntity(_testData.BowlerWithMultipleIdentities!.PlayerRoute!, "players")).Returns(_testData.BowlerWithMultipleIdentities!.PlayerRoute!);
            var playerDataSource = new SqlServerPlayerDataSource(_connectionFactory, _routeNormaliser.Object, _statisticsQueryBuilder.Object);
            _statisticsQueryBuilder.Setup(x => x.BuildWhereClause(It.IsAny<StatisticsFilter>())).Returns((string.Empty, new Dictionary<string, object>()));

            var result = await playerDataSource.ReadPlayerByRoute(_testData.BowlerWithMultipleIdentities.PlayerRoute!, null);

            Assert.NotNull(result);
            Assert.Equal(_testData.BowlerWithMultipleIdentities.PlayerId, result!.PlayerId);
            Assert.Equal(_testData.BowlerWithMultipleIdentities.PlayerRoute, result.PlayerRoute);
            Assert.Equal(_testData.BowlerWithMultipleIdentities.MemberKey, result.MemberKey);
            Assert.Equal(_testData.BowlerWithMultipleIdentities.PlayerIdentities.Count, result.PlayerIdentities.Count);
            foreach (var identity in _testData.BowlerWithMultipleIdentities.PlayerIdentities)
            {
                var resultIdentity = result.PlayerIdentities.SingleOrDefault(x => x.PlayerIdentityId == identity.PlayerIdentityId);
                Assert.NotNull(resultIdentity);
                Assert.NotNull(resultIdentity!.Team);

                Assert.Equal(identity.PlayerIdentityName, resultIdentity!.PlayerIdentityName);
                Assert.Equal(identity.LinkedBy, resultIdentity.LinkedBy);
                Assert.Equal(identity.Team!.TeamId, resultIdentity.Team!.TeamId);
                Assert.Equal(identity.Team.TeamName, resultIdentity.Team.TeamName);
                Assert.Equal(identity.Team.TeamRoute, resultIdentity.Team.TeamRoute);
                Assert.Equal(identity.TotalMatches, resultIdentity.TotalMatches);
                Assert.Equal(identity.FirstPlayed!.Value.AccurateToTheMinute(), resultIdentity.FirstPlayed!.Value.AccurateToTheMinute());
                Assert.Equal(identity.LastPlayed!.Value.AccurateToTheMinute(), resultIdentity.LastPlayed!.Value.AccurateToTheMinute());
            }
        }

        [Fact]
        public async Task Read_player_by_route_supports_filter_statistics_by_date_and_returns_all_identities()
        {
            _routeNormaliser.Setup(x => x.NormaliseRouteToEntity(_testData.BowlerWithMultipleIdentities!.PlayerRoute!, "players")).Returns(_testData.BowlerWithMultipleIdentities!.PlayerRoute!);
            var playerDataSource = new SqlServerPlayerDataSource(_connectionFactory, _routeNormaliser.Object, _statisticsQueryBuilder.Object);

            var firstMatch = _testData.BowlerWithMultipleIdentities.PlayerIdentities.Min(x => x.FirstPlayed);
            var lastMatch = _testData.BowlerWithMultipleIdentities.PlayerIdentities.Min(x => x.FirstPlayed);
            var midDate = lastMatch - ((lastMatch - firstMatch) / 2);
            var filter = new StatisticsFilter { FromDate = firstMatch, UntilDate = midDate };
            _statisticsQueryBuilder.Setup(x => x.BuildWhereClause(It.IsAny<StatisticsFilter>())).Returns((" AND MatchStartTime >= @FromDate AND MatchStartTime <= @UntilDate", new Dictionary<string, object> { { "FromDate", filter.FromDate! }, { "UntilDate", filter.UntilDate! } }));

            var result = await playerDataSource.ReadPlayerByRoute(_testData.BowlerWithMultipleIdentities.PlayerRoute!, filter);

            Assert.NotNull(result);
            Assert.Equal(_testData.BowlerWithMultipleIdentities.PlayerId, result!.PlayerId);
            Assert.Equal(_testData.BowlerWithMultipleIdentities.PlayerIdentities.Count, result.PlayerIdentities.Count);
            foreach (var identity in _testData.BowlerWithMultipleIdentities.PlayerIdentities)
            {
                var resultIdentity = result.PlayerIdentities.SingleOrDefault(x => x.PlayerIdentityId == identity.PlayerIdentityId);
                Assert.NotNull(resultIdentity);

                if (identity.FirstPlayed > midDate)
                {
                    Assert.Null(resultIdentity!.FirstPlayed);
                }
                else
                {
                    Assert.Equal(identity.FirstPlayed!.Value.AccurateToTheMinute(), resultIdentity!.FirstPlayed!.Value.AccurateToTheMinute());
                }
                if (identity.LastPlayed > midDate)
                {
                    if (identity.FirstPlayed <= midDate)
                    {
                        Assert.True(resultIdentity.LastPlayed >= identity.FirstPlayed);
                        Assert.True(resultIdentity.LastPlayed <= midDate);
                    }
                    else
                    {
                        Assert.Null(resultIdentity.LastPlayed);
                    }
                }
                else
                {
                    Assert.Equal(identity.LastPlayed!.Value.AccurateToTheMinute(), resultIdentity.LastPlayed!.Value.AccurateToTheMinute());
                }
            }
        }

        [Fact]
        public async Task Read_player_by_route_returns_all_identities_when_statistics_are_excluded_by_filter()
        {
            _routeNormaliser.Setup(x => x.NormaliseRouteToEntity(_testData.BowlerWithMultipleIdentities!.PlayerRoute!, "players")).Returns(_testData.BowlerWithMultipleIdentities!.PlayerRoute!);
            var playerDataSource = new SqlServerPlayerDataSource(_connectionFactory, _routeNormaliser.Object, _statisticsQueryBuilder.Object);

            var firstMatch = _testData.BowlerWithMultipleIdentities.PlayerIdentities.Min(x => x.FirstPlayed)!;
            var filter = new StatisticsFilter { FromDate = firstMatch.Value.AddDays(-1), UntilDate = firstMatch.Value.AddDays(-1) };
            _statisticsQueryBuilder.Setup(x => x.BuildWhereClause(It.IsAny<StatisticsFilter>())).Returns((" AND MatchStartTime >= @FromDate AND MatchStartTime <= @UntilDate", new Dictionary<string, object> { { "FromDate", filter.FromDate }, { "UntilDate", filter.UntilDate } }));

            var result = await playerDataSource.ReadPlayerByRoute(_testData.BowlerWithMultipleIdentities.PlayerRoute!, filter);

            Assert.NotNull(result);
            Assert.Equal(_testData.BowlerWithMultipleIdentities.PlayerId, result!.PlayerId);
            Assert.Equal(_testData.BowlerWithMultipleIdentities.PlayerIdentities.Count, result.PlayerIdentities.Count);
            foreach (var identity in _testData.BowlerWithMultipleIdentities.PlayerIdentities)
            {
                var resultIdentity = result.PlayerIdentities.SingleOrDefault(x => x.PlayerIdentityId == identity.PlayerIdentityId);
                Assert.NotNull(resultIdentity);

                Assert.Equal(0, resultIdentity!.TotalMatches);
                Assert.Null(resultIdentity.FirstPlayed);
                Assert.Null(resultIdentity.LastPlayed);
            }
        }

        [Fact]
        public async Task ReadPlayerByMemberKey_returns_PlayerRoute_for_matching_player()
        {
            var playerDataSource = new SqlServerPlayerDataSource(_connectionFactory, _routeNormaliser.Object, _statisticsQueryBuilder.Object);
            var player = _testData.PlayersWithMultipleIdentities.First(p => p.MemberKey.HasValue);

            var result = await playerDataSource.ReadPlayerByMemberKey(player.MemberKey!.Value);

            Assert.NotNull(result);
            Assert.Equal(player.PlayerRoute, result!.PlayerRoute);
        }

        [Fact]
        public async Task ReadPlayerByMemberKey_returns_null_for_no_match()
        {
            var playerDataSource = new SqlServerPlayerDataSource(_connectionFactory, _routeNormaliser.Object, _statisticsQueryBuilder.Object);

            var result = await playerDataSource.ReadPlayerByMemberKey(Guid.NewGuid());

            Assert.Null(result);
        }

        [Fact]
        public async Task ReadPlayerIdentityByRoute_returns_identity_player_team_and_club()
        {
            var playerDataSource = new SqlServerPlayerDataSource(_connectionFactory, _routeNormaliser.Object, _statisticsQueryBuilder.Object);

            var foundAtLeastOneClub = false;
            foreach (var identity in _testData.PlayerIdentities)
            {
                var route = $"{identity.Team!.TeamRoute}/edit/players/{identity.RouteSegment}/rename";
                _routeNormaliser.Setup(x => x.NormaliseRouteToEntity(route, "/teams")).Returns(identity.Team!.TeamRoute!);

                var result = await playerDataSource.ReadPlayerIdentityByRoute(route).ConfigureAwait(false);

                Assert.NotNull(result?.Team);
                Assert.Equal(identity.PlayerIdentityId, result!.PlayerIdentityId);
                Assert.Equal(identity.PlayerIdentityName, result.PlayerIdentityName);
                Assert.Equal(identity.RouteSegment, result.RouteSegment);
                Assert.Equal(identity.Player?.PlayerId, result.Player?.PlayerId);
                Assert.Equal(identity.Player?.PlayerRoute, result.Player?.PlayerRoute);
                Assert.Equal(identity.Team.TeamId, result.Team!.TeamId);
                Assert.Equal(identity.Team.TeamName, result.Team!.TeamName);
                Assert.Equal(identity.Team.TeamRoute, result.Team!.TeamRoute);
                Assert.Equal(identity.Team.Club?.ClubId, result.Team.Club?.ClubId);
                Assert.Equal(identity.Team.Club?.ClubName, result.Team.Club?.ClubName);
                Assert.Equal(identity.Team.Club?.ClubRoute, result.Team.Club?.ClubRoute);

                foundAtLeastOneClub = foundAtLeastOneClub || identity.Team.Club != null;
            }
            Assert.True(foundAtLeastOneClub);
        }

        [Fact]
        public async Task ReadPlayerIdentityByRoute_returns_null_for_no_match()
        {
            var playerDataSource = new SqlServerPlayerDataSource(_connectionFactory, _routeNormaliser.Object, _statisticsQueryBuilder.Object);
            var team = _testData.Teams.First();
            var route = $"{team.TeamRoute}/edit/players/identity-that-does-not-exist/rename";
            _routeNormaliser.Setup(x => x.NormaliseRouteToEntity(route, "/teams")).Returns(team.TeamRoute!);

            var result = await playerDataSource.ReadPlayerIdentityByRoute(route).ConfigureAwait(false);

            Assert.Null(result);
        }

        [Theory]
        [InlineData("/teams/example/some-player")]
        [InlineData("/teams/example/players/some-player")]
        [InlineData("/teams/example/players/edit/some-player")]
        [InlineData("/teams/example/players/edit/some-player/invalid")]
        public async Task ReadPlayerIdentityByRoute_throws_ArgumentException_for_invalid_route(string route)
        {
            var playerDataSource = new SqlServerPlayerDataSource(_connectionFactory, _routeNormaliser.Object, _statisticsQueryBuilder.Object);
            var identity = _testData.PlayerIdentities.First();
            _routeNormaliser.Setup(x => x.NormaliseRouteToEntity(route, "/teams")).Returns("/teams/example");

            await Assert.ThrowsAsync<ArgumentException>(async () => _ = await playerDataSource.ReadPlayerIdentityByRoute(route).ConfigureAwait(false)).ConfigureAwait(false);
        }
    }
}
