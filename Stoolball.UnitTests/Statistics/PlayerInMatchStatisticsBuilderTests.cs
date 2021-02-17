using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using Stoolball.Matches;
using Stoolball.Statistics;
using Stoolball.Teams;
using Xunit;

namespace Stoolball.UnitTests.Statistics
{
    public class PlayerInMatchStatisticsBuilderTests : IClassFixture<MatchFixture>
    {
        private readonly MatchFixture _matchFixture;
        private List<PlayerIdentity> _playerIdentities = new List<PlayerIdentity>();

        public PlayerInMatchStatisticsBuilderTests(MatchFixture matchFixture)
        {
            if (matchFixture is null || matchFixture.Match is null)
            {
                throw new System.ArgumentNullException(nameof(matchFixture));
            }

            _matchFixture = matchFixture;
            _playerIdentities.AddRange(matchFixture.HomePlayers);
            _playerIdentities.AddRange(matchFixture.AwayPlayers);
        }

        private static void AssertPlayerFields(PlayerIdentity identity, PlayerInMatchStatisticsRecord playerRecord)
        {
            Assert.Equal(identity.Player?.PlayerId, playerRecord.PlayerId);
            Assert.Equal(identity.PlayerIdentityName, playerRecord.PlayerIdentityName);
            Assert.Equal(identity.Player?.PlayerRoute, playerRecord.PlayerRoute);
        }

        private void AssertMatchFields(PlayerInMatchStatisticsRecord playerRecord)
        {
            Assert.Equal(_matchFixture.Match.MatchId, playerRecord.MatchId);
            Assert.Equal(_matchFixture.Match.MatchName, playerRecord.MatchName);
            Assert.Equal(_matchFixture.Match.MatchType, playerRecord.MatchType);
            Assert.Equal(_matchFixture.Match.PlayerType, playerRecord.MatchPlayerType);
            Assert.Equal(_matchFixture.Match.MatchRoute, playerRecord.MatchRoute);
            Assert.Equal(_matchFixture.Match.StartTime, playerRecord.MatchStartTime);
            Assert.Equal(_matchFixture.Match.Tournament?.TournamentId, playerRecord.TournamentId);
            Assert.Equal(_matchFixture.Match.MatchLocation.MatchLocationId, playerRecord.MatchLocationId);
            Assert.Equal(_matchFixture.Match.Season.SeasonId, playerRecord.SeasonId);
            Assert.Equal(_matchFixture.Match.Season.Competition.CompetitionId, playerRecord.CompetitionId);
        }

        private void AssertInningsFields(PlayerIdentity batter, MatchInnings innings, PlayerInMatchStatisticsRecord playerRecord)
        {
            var isOnBattingTeam = innings.BattingTeam.Team.TeamId == batter.Team.TeamId;
            if (isOnBattingTeam)
            {
                Assert.Equal(1, playerRecord.PlayerInningsInMatchInnings);
            }
            else
            {
                Assert.Null(playerRecord.PlayerInningsInMatchInnings);
            }

            Assert.Equal(innings.MatchInningsId, playerRecord.MatchInningsId);
            Assert.Equal(innings.InningsOrderInMatch, playerRecord.InningsOrderInMatch);
            Assert.Equal(_matchFixture.Match.InningsOrderIsKnown, playerRecord.InningsOrderIsKnown);
            Assert.Equal(innings.Runs, playerRecord.MatchInningsRuns);
            Assert.Equal(innings.Wickets, playerRecord.MatchInningsWickets);

            Assert.Equal(isOnBattingTeam ? innings.BattingTeam.MatchTeamId : innings.BowlingTeam.MatchTeamId, playerRecord.MatchTeamId);
            Assert.Equal(isOnBattingTeam ? innings.BattingTeam.Team.TeamId : innings.BowlingTeam.Team.TeamId, playerRecord.TeamId);
            Assert.Equal(isOnBattingTeam ? innings.BattingTeam.Team.TeamName : innings.BowlingTeam.Team.TeamName, playerRecord.TeamName);
            Assert.Equal(isOnBattingTeam ? innings.BattingTeam.Team.TeamRoute : innings.BowlingTeam.Team.TeamRoute, playerRecord.TeamRoute);

            Assert.Equal(isOnBattingTeam ? innings.BowlingTeam.Team.TeamId : innings.BattingTeam.Team.TeamId, playerRecord.OppositionTeamId);
            Assert.Equal(isOnBattingTeam ? innings.BowlingTeam.Team.TeamName : innings.BattingTeam.Team.TeamName, playerRecord.OppositionTeamName);
            Assert.Equal(isOnBattingTeam ? innings.BowlingTeam.Team.TeamRoute : innings.BattingTeam.Team.TeamRoute, playerRecord.OppositionTeamRoute);
        }

        [Fact]
        public void Each_batter_should_have_one_record_per_innings_with_player_and_match_data()
        {
            var batters = _matchFixture.Match.MatchInnings.SelectMany(i => i.PlayerInnings.Select(pi => pi.PlayerIdentity));

            var finder = new Mock<IPlayerIdentityFinder>();
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match)).Returns(_playerIdentities);
            var result = new PlayerInMatchStatisticsBuilder(finder.Object).BuildStatisticsForMatch(_matchFixture.Match);

            foreach (var batter in batters)
            {
                foreach (var innings in _matchFixture.Match.MatchInnings)
                {
                    var playerRecord = result.SingleOrDefault(x => x.PlayerIdentityId == batter.PlayerIdentityId && x.MatchInningsId == innings.MatchInningsId && (x.PlayerInningsInMatchInnings == 1 || x.PlayerInningsInMatchInnings == null));
                    Assert.NotNull(playerRecord);

                    AssertPlayerFields(batter, playerRecord);
                    AssertMatchFields(playerRecord);
                }
            }
        }

        [Fact]
        public void Each_batter_should_have_one_record_per_innings_with_innings_data()
        {
            var batters = _matchFixture.Match.MatchInnings.SelectMany(i => i.PlayerInnings.Select(pi => pi.PlayerIdentity));

            var finder = new Mock<IPlayerIdentityFinder>();
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match)).Returns(_playerIdentities);
            var result = new PlayerInMatchStatisticsBuilder(finder.Object).BuildStatisticsForMatch(_matchFixture.Match);

            foreach (var batter in batters)
            {
                foreach (var innings in _matchFixture.Match.MatchInnings)
                {
                    var playerRecord = result.SingleOrDefault(x => x.PlayerIdentityId == batter.PlayerIdentityId && x.MatchInningsId == innings.MatchInningsId && (x.PlayerInningsInMatchInnings == 1 || x.PlayerInningsInMatchInnings == null));
                    Assert.NotNull(playerRecord);

                    AssertInningsFields(batter, innings, playerRecord);
                }
            }
        }

        [Fact]
        public void Each_fielder_should_have_one_record_per_innings_with_player_and_match_data()
        {
            var fielders = _matchFixture.Match.MatchInnings.SelectMany(i => i.PlayerInnings.Where(pi => pi.DismissedBy != null).Select(pi => pi.DismissedBy));

            var finder = new Mock<IPlayerIdentityFinder>();
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match)).Returns(_playerIdentities);
            var result = new PlayerInMatchStatisticsBuilder(finder.Object).BuildStatisticsForMatch(_matchFixture.Match);

            foreach (var identity in fielders)
            {
                foreach (var innings in _matchFixture.Match.MatchInnings)
                {
                    var playerRecord = result.SingleOrDefault(x => x.PlayerIdentityId == identity.PlayerIdentityId && x.MatchInningsId == innings.MatchInningsId && (x.PlayerInningsInMatchInnings == 1 || x.PlayerInningsInMatchInnings == null));
                    Assert.NotNull(playerRecord);

                    AssertPlayerFields(identity, playerRecord);
                    AssertMatchFields(playerRecord);
                }
            }
        }

        [Fact]
        public void Each_fielder_should_have_one_record_per_innings_with_innings_data()
        {
            var fielders = _matchFixture.Match.MatchInnings.SelectMany(i => i.PlayerInnings.Where(pi => pi.DismissedBy != null).Select(pi => pi.DismissedBy));

            var finder = new Mock<IPlayerIdentityFinder>();
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match)).Returns(_playerIdentities);
            var result = new PlayerInMatchStatisticsBuilder(finder.Object).BuildStatisticsForMatch(_matchFixture.Match);

            foreach (var identity in fielders)
            {
                foreach (var innings in _matchFixture.Match.MatchInnings)
                {
                    var playerRecord = result.SingleOrDefault(x => x.PlayerIdentityId == identity.PlayerIdentityId && x.MatchInningsId == innings.MatchInningsId && (x.PlayerInningsInMatchInnings == 1 || x.PlayerInningsInMatchInnings == null));
                    Assert.NotNull(playerRecord);

                    AssertInningsFields(identity, innings, playerRecord);
                }
            }
        }

        [Fact]
        public void Each_wicket_taker_should_have_one_record_per_innings_with_player_and_match_data()
        {
            var wicketTakers = _matchFixture.Match.MatchInnings.SelectMany(i => i.PlayerInnings.Where(pi => pi.Bowler != null).Select(pi => pi.Bowler));

            var finder = new Mock<IPlayerIdentityFinder>();
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match)).Returns(_playerIdentities);
            var result = new PlayerInMatchStatisticsBuilder(finder.Object).BuildStatisticsForMatch(_matchFixture.Match);

            foreach (var identity in wicketTakers)
            {
                foreach (var innings in _matchFixture.Match.MatchInnings)
                {
                    var playerRecord = result.SingleOrDefault(x => x.PlayerIdentityId == identity.PlayerIdentityId && x.MatchInningsId == innings.MatchInningsId && (x.PlayerInningsInMatchInnings == 1 || x.PlayerInningsInMatchInnings == null));
                    Assert.NotNull(playerRecord);

                    AssertPlayerFields(identity, playerRecord);
                    AssertMatchFields(playerRecord);
                }
            }
        }

        [Fact]
        public void Each_wicket_taker_should_have_one_record_per_innings_with_innings_data()
        {
            var wicketTakers = _matchFixture.Match.MatchInnings.SelectMany(i => i.PlayerInnings.Where(pi => pi.Bowler != null).Select(pi => pi.Bowler));

            var finder = new Mock<IPlayerIdentityFinder>();
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match)).Returns(_playerIdentities);
            var result = new PlayerInMatchStatisticsBuilder(finder.Object).BuildStatisticsForMatch(_matchFixture.Match);

            foreach (var identity in wicketTakers)
            {
                foreach (var innings in _matchFixture.Match.MatchInnings)
                {
                    var playerRecord = result.SingleOrDefault(x => x.PlayerIdentityId == identity.PlayerIdentityId && x.MatchInningsId == innings.MatchInningsId && (x.PlayerInningsInMatchInnings == 1 || x.PlayerInningsInMatchInnings == null));
                    Assert.NotNull(playerRecord);

                    AssertInningsFields(identity, innings, playerRecord);
                }
            }
        }

        [Fact]
        public void Each_bowler_with_figures_should_have_one_record_per_innings_with_player_and_match_data()
        {
            var bowlers = _matchFixture.Match.MatchInnings.SelectMany(i => i.BowlingFigures.Select(pi => pi.Bowler));

            var finder = new Mock<IPlayerIdentityFinder>();
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match)).Returns(_playerIdentities);
            var result = new PlayerInMatchStatisticsBuilder(finder.Object).BuildStatisticsForMatch(_matchFixture.Match);

            foreach (var identity in bowlers)
            {
                foreach (var innings in _matchFixture.Match.MatchInnings)
                {
                    var playerRecord = result.SingleOrDefault(x => x.PlayerIdentityId == identity.PlayerIdentityId && x.MatchInningsId == innings.MatchInningsId && (x.PlayerInningsInMatchInnings == 1 || x.PlayerInningsInMatchInnings == null));
                    Assert.NotNull(playerRecord);

                    AssertPlayerFields(identity, playerRecord);
                    AssertMatchFields(playerRecord);
                }
            }
        }

        [Fact]
        public void Each_bowler_with_figures_should_have_one_record_per_innings_with_innings_data()
        {
            var bowlers = _matchFixture.Match.MatchInnings.SelectMany(i => i.BowlingFigures.Select(pi => pi.Bowler));

            var finder = new Mock<IPlayerIdentityFinder>();
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match)).Returns(_playerIdentities);
            var result = new PlayerInMatchStatisticsBuilder(finder.Object).BuildStatisticsForMatch(_matchFixture.Match);

            foreach (var identity in bowlers)
            {
                foreach (var innings in _matchFixture.Match.MatchInnings)
                {
                    var playerRecord = result.SingleOrDefault(x => x.PlayerIdentityId == identity.PlayerIdentityId && x.MatchInningsId == innings.MatchInningsId && (x.PlayerInningsInMatchInnings == 1 || x.PlayerInningsInMatchInnings == null));
                    Assert.NotNull(playerRecord);

                    AssertInningsFields(identity, innings, playerRecord);
                }
            }
        }

        [Fact]
        public void Each_bowler_with_overs_should_have_one_record_per_innings_with_player_and_match_data()
        {
            var bowlers = _matchFixture.Match.MatchInnings.SelectMany(i => i.OversBowled.Select(pi => pi.PlayerIdentity));

            var finder = new Mock<IPlayerIdentityFinder>();
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match)).Returns(_playerIdentities);
            var result = new PlayerInMatchStatisticsBuilder(finder.Object).BuildStatisticsForMatch(_matchFixture.Match);

            foreach (var identity in bowlers)
            {
                foreach (var innings in _matchFixture.Match.MatchInnings)
                {
                    var playerRecord = result.SingleOrDefault(x => x.PlayerIdentityId == identity.PlayerIdentityId && x.MatchInningsId == innings.MatchInningsId && (x.PlayerInningsInMatchInnings == 1 || x.PlayerInningsInMatchInnings == null));
                    Assert.NotNull(playerRecord);

                    AssertPlayerFields(identity, playerRecord);
                    AssertMatchFields(playerRecord);
                }
            }
        }

        [Fact]
        public void Each_bowler_with_overs_should_have_one_record_per_innings_with_innings_data()
        {
            var bowlers = _matchFixture.Match.MatchInnings.SelectMany(i => i.OversBowled.Select(pi => pi.PlayerIdentity));

            var finder = new Mock<IPlayerIdentityFinder>();
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match)).Returns(_playerIdentities);
            var result = new PlayerInMatchStatisticsBuilder(finder.Object).BuildStatisticsForMatch(_matchFixture.Match);

            foreach (var identity in bowlers)
            {
                foreach (var innings in _matchFixture.Match.MatchInnings)
                {
                    var playerRecord = result.SingleOrDefault(x => x.PlayerIdentityId == identity.PlayerIdentityId && x.MatchInningsId == innings.MatchInningsId && (x.PlayerInningsInMatchInnings == 1 || x.PlayerInningsInMatchInnings == null));
                    Assert.NotNull(playerRecord);

                    AssertInningsFields(identity, innings, playerRecord);
                }
            }
        }

        [Fact]
        public void Each_award_winner_should_have_one_record_per_innings_with_player_and_match_data()
        {
            var winners = _matchFixture.Match.Awards.Select(pi => pi.PlayerIdentity);

            var finder = new Mock<IPlayerIdentityFinder>();
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match)).Returns(_playerIdentities);
            var result = new PlayerInMatchStatisticsBuilder(finder.Object).BuildStatisticsForMatch(_matchFixture.Match);

            foreach (var identity in winners)
            {
                foreach (var innings in _matchFixture.Match.MatchInnings)
                {
                    var playerRecord = result.SingleOrDefault(x => x.PlayerIdentityId == identity.PlayerIdentityId && x.MatchInningsId == innings.MatchInningsId && (x.PlayerInningsInMatchInnings == 1 || x.PlayerInningsInMatchInnings == null));
                    Assert.NotNull(playerRecord);

                    AssertPlayerFields(identity, playerRecord);
                    AssertMatchFields(playerRecord);
                }
            }
        }

        [Fact]
        public void Each_award_winner_should_have_one_record_per_innings_with_innings_data()
        {
            var winners = _matchFixture.Match.Awards.Select(pi => pi.PlayerIdentity);

            var finder = new Mock<IPlayerIdentityFinder>();
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match)).Returns(_playerIdentities);
            var result = new PlayerInMatchStatisticsBuilder(finder.Object).BuildStatisticsForMatch(_matchFixture.Match);

            foreach (var identity in winners)
            {
                foreach (var innings in _matchFixture.Match.MatchInnings)
                {
                    var playerRecord = result.SingleOrDefault(x => x.PlayerIdentityId == identity.PlayerIdentityId && x.MatchInningsId == innings.MatchInningsId && (x.PlayerInningsInMatchInnings == 1 || x.PlayerInningsInMatchInnings == null));
                    Assert.NotNull(playerRecord);

                    AssertInningsFields(identity, innings, playerRecord);
                }
            }
        }

        [Fact]
        public void Only_players_who_bat_again_in_an_innings_should_have_multiple_records_for_an_innings()
        {
            var finder = new Mock<IPlayerIdentityFinder>();
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match)).Returns(_playerIdentities);
            var result = new PlayerInMatchStatisticsBuilder(finder.Object).BuildStatisticsForMatch(_matchFixture.Match);

            foreach (var identity in _playerIdentities)
            {
                foreach (var innings in _matchFixture.Match.MatchInnings)
                {
                    // In the test data, the first batter bats twice in each innings
                    if (identity.PlayerIdentityId == innings.PlayerInnings[0].PlayerIdentity.PlayerIdentityId)
                    {
                        Assert.Equal(2, result.Count(x => x.PlayerIdentityId == identity.PlayerIdentityId && x.MatchInningsId == innings.MatchInningsId));
                        Assert.NotNull(result.SingleOrDefault(x => x.PlayerIdentityId == identity.PlayerIdentityId && x.MatchInningsId == innings.MatchInningsId && x.PlayerInningsInMatchInnings == 1));
                        Assert.NotNull(result.SingleOrDefault(x => x.PlayerIdentityId == identity.PlayerIdentityId && x.MatchInningsId == innings.MatchInningsId && x.PlayerInningsInMatchInnings == 2));
                    }
                    else
                    {
                        Assert.NotNull(result.Single(x => x.PlayerIdentityId == identity.PlayerIdentityId && x.MatchInningsId == innings.MatchInningsId));
                    }
                }
            }
        }

        [Fact]
        public void Each_batter_in_batting_innings_should_have_position_dismissal_and_score()
        {
            var batters = _matchFixture.Match.MatchInnings.SelectMany(i => i.PlayerInnings.Select(pi => pi.PlayerIdentity));

            var finder = new Mock<IPlayerIdentityFinder>();
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match)).Returns(_playerIdentities);
            var result = new PlayerInMatchStatisticsBuilder(finder.Object).BuildStatisticsForMatch(_matchFixture.Match);

            foreach (var batter in batters)
            {
                foreach (var innings in _matchFixture.Match.MatchInnings.Where(x => x.BattingTeam.Team.TeamId == batter.Team.TeamId))
                {
                    var expected = innings.PlayerInnings.Where(x => x.PlayerIdentity.PlayerIdentityId == batter.PlayerIdentityId).ToList();
                    var playerRecords = result.Where(x => x.PlayerIdentityId == batter.PlayerIdentityId && x.MatchInningsId == innings.MatchInningsId);

                    Assert.Equal(expected.Count, playerRecords.Count());
                    for (var i = 0; i < expected.Count; i++)
                    {
                        var playerRecord = playerRecords.SingleOrDefault(x => x.BattingPosition == expected[i].BattingPosition);
                        Assert.NotNull(playerRecord);

                        Assert.Equal(expected[i].DismissalType, playerRecord.DismissalType);
                        Assert.Equal(StatisticsConstants.DISMISSALS_THAT_ARE_OUT.Contains(expected[i].DismissalType), playerRecord.PlayerWasDismissed);
                        Assert.Equal(expected[i].RunsScored, playerRecord.RunsScored);
                        Assert.Equal(expected[i].BallsFaced, playerRecord.BallsFaced);
                    }
                }
            }
        }

        [Fact]
        public void Batting_team_members_with_no_player_innings_did_not_bat_and_should_have_null_position_and_score()
        {
            var batters = _matchFixture.Match.MatchInnings.SelectMany(i => i.PlayerInnings.Select(pi => pi.PlayerIdentity.PlayerIdentityId.Value)).ToList();
            var missingBatters = _playerIdentities.Where(x => !batters.Contains(x.PlayerIdentityId.Value));

            var finder = new Mock<IPlayerIdentityFinder>();
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match)).Returns(_playerIdentities);
            var result = new PlayerInMatchStatisticsBuilder(finder.Object).BuildStatisticsForMatch(_matchFixture.Match);

            foreach (var batter in missingBatters)
            {
                foreach (var innings in _matchFixture.Match.MatchInnings.Where(x => x.BattingTeam.Team.TeamId == batter.Team.TeamId))
                {
                    var playerRecord = result.SingleOrDefault(x => x.PlayerIdentityId == batter.PlayerIdentityId && x.MatchInningsId == innings.MatchInningsId);
                    Assert.NotNull(playerRecord);

                    Assert.Null(playerRecord.BattingPosition);
                    Assert.Equal(DismissalType.DidNotBat, playerRecord.DismissalType);
                    Assert.False(playerRecord.PlayerWasDismissed);
                    Assert.Null(playerRecord.RunsScored);
                    Assert.Null(playerRecord.BallsFaced);
                }
            }
        }

        [Fact]
        public void Each_dismissal_credited_to_a_bowler_should_have_bowler_details()
        {
            var finder = new Mock<IPlayerIdentityFinder>();
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match)).Returns(_playerIdentities);
            var result = new PlayerInMatchStatisticsBuilder(finder.Object).BuildStatisticsForMatch(_matchFixture.Match);

            foreach (var innings in _matchFixture.Match.MatchInnings)
            {
                var playerInningsCountForPlayers = new Dictionary<Guid, int>();
                foreach (var playerInnings in innings.PlayerInnings)
                {
                    if (!playerInningsCountForPlayers.ContainsKey(playerInnings.PlayerIdentity.PlayerIdentityId.Value))
                    {
                        playerInningsCountForPlayers.Add(playerInnings.PlayerIdentity.PlayerIdentityId.Value, 1);
                    }
                    else
                    {
                        playerInningsCountForPlayers[playerInnings.PlayerIdentity.PlayerIdentityId.Value]++;
                    }

                    var playerRecord = result.SingleOrDefault(x => x.PlayerIdentityId == playerInnings.PlayerIdentity.PlayerIdentityId && x.MatchInningsId == innings.MatchInningsId && x.PlayerInningsInMatchInnings == playerInningsCountForPlayers[playerInnings.PlayerIdentity.PlayerIdentityId.Value]);
                    Assert.NotNull(playerRecord);

                    if (StatisticsConstants.DISMISSALS_CREDITED_TO_BOWLER.Contains(playerInnings.DismissalType) && playerInnings.Bowler != null)
                    {
                        Assert.Equal(playerInnings.Bowler.PlayerIdentityId, playerRecord.BowledByPlayerIdentityId);
                        Assert.Equal(playerInnings.Bowler.PlayerIdentityName, playerRecord.BowledByPlayerIdentityName);
                        Assert.Equal(playerInnings.Bowler.Player.PlayerRoute, playerRecord.BowledByPlayerRoute);
                    }
                    else
                    {
                        Assert.Null(playerRecord.BowledByPlayerIdentityId);
                        Assert.Null(playerRecord.BowledByPlayerIdentityName);
                        Assert.Null(playerRecord.BowledByPlayerRoute);
                    }
                }
            }
        }

        [Fact]
        public void Each_caught_dismissal_should_have_catcher_details()
        {
            var finder = new Mock<IPlayerIdentityFinder>();
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match)).Returns(_playerIdentities);
            var result = new PlayerInMatchStatisticsBuilder(finder.Object).BuildStatisticsForMatch(_matchFixture.Match);

            foreach (var innings in _matchFixture.Match.MatchInnings)
            {
                var playerInningsCountForPlayers = new Dictionary<Guid, int>();
                foreach (var playerInnings in innings.PlayerInnings)
                {
                    if (!playerInningsCountForPlayers.ContainsKey(playerInnings.PlayerIdentity.PlayerIdentityId.Value))
                    {
                        playerInningsCountForPlayers.Add(playerInnings.PlayerIdentity.PlayerIdentityId.Value, 1);
                    }
                    else
                    {
                        playerInningsCountForPlayers[playerInnings.PlayerIdentity.PlayerIdentityId.Value]++;
                    }

                    var playerRecord = result.SingleOrDefault(x => x.PlayerIdentityId == playerInnings.PlayerIdentity.PlayerIdentityId && x.MatchInningsId == innings.MatchInningsId && x.PlayerInningsInMatchInnings == playerInningsCountForPlayers[playerInnings.PlayerIdentity.PlayerIdentityId.Value]);
                    Assert.NotNull(playerRecord);

                    if (playerInnings.DismissalType == DismissalType.Caught && playerInnings.DismissedBy != null)
                    {
                        Assert.Equal(playerInnings.DismissedBy.PlayerIdentityId, playerRecord.CaughtByPlayerIdentityId);
                        Assert.Equal(playerInnings.DismissedBy.PlayerIdentityName, playerRecord.CaughtByPlayerIdentityName);
                        Assert.Equal(playerInnings.DismissedBy.Player.PlayerRoute, playerRecord.CaughtByPlayerRoute);
                    }
                    else if (playerInnings.DismissalType == DismissalType.CaughtAndBowled && playerInnings.Bowler != null)
                    {
                        Assert.Equal(playerInnings.Bowler.PlayerIdentityId, playerRecord.CaughtByPlayerIdentityId);
                        Assert.Equal(playerInnings.Bowler.PlayerIdentityName, playerRecord.CaughtByPlayerIdentityName);
                        Assert.Equal(playerInnings.Bowler.Player.PlayerRoute, playerRecord.CaughtByPlayerRoute);
                    }
                    else
                    {
                        Assert.Null(playerRecord.CaughtByPlayerIdentityId);
                        Assert.Null(playerRecord.CaughtByPlayerIdentityName);
                        Assert.Null(playerRecord.CaughtByPlayerRoute);
                    }
                }
            }
        }

        [Fact]
        public void Each_run_out_dismissal_should_have_fielder_details()
        {
            var finder = new Mock<IPlayerIdentityFinder>();
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match)).Returns(_playerIdentities);
            var result = new PlayerInMatchStatisticsBuilder(finder.Object).BuildStatisticsForMatch(_matchFixture.Match);

            foreach (var innings in _matchFixture.Match.MatchInnings)
            {
                var playerInningsCountForPlayers = new Dictionary<Guid, int>();
                foreach (var playerInnings in innings.PlayerInnings)
                {
                    if (!playerInningsCountForPlayers.ContainsKey(playerInnings.PlayerIdentity.PlayerIdentityId.Value))
                    {
                        playerInningsCountForPlayers.Add(playerInnings.PlayerIdentity.PlayerIdentityId.Value, 1);
                    }
                    else
                    {
                        playerInningsCountForPlayers[playerInnings.PlayerIdentity.PlayerIdentityId.Value]++;
                    }

                    var playerRecord = result.SingleOrDefault(x => x.PlayerIdentityId == playerInnings.PlayerIdentity.PlayerIdentityId && x.MatchInningsId == innings.MatchInningsId && x.PlayerInningsInMatchInnings == playerInningsCountForPlayers[playerInnings.PlayerIdentity.PlayerIdentityId.Value]);
                    Assert.NotNull(playerRecord);

                    if (playerInnings.DismissalType == DismissalType.RunOut && playerInnings.DismissedBy != null)
                    {
                        Assert.Equal(playerInnings.DismissedBy.PlayerIdentityId, playerRecord.RunOutByPlayerIdentityId);
                        Assert.Equal(playerInnings.DismissedBy.PlayerIdentityName, playerRecord.RunOutByPlayerIdentityName);
                        Assert.Equal(playerInnings.DismissedBy.Player.PlayerRoute, playerRecord.RunOutByPlayerRoute);
                    }
                    else
                    {
                        Assert.Null(playerRecord.RunOutByPlayerIdentityId);
                        Assert.Null(playerRecord.RunOutByPlayerIdentityName);
                        Assert.Null(playerRecord.RunOutByPlayerRoute);
                    }
                }
            }
        }

        [Fact]
        public void Fielding_team_members_should_have_catches_and_run_outs()
        {
            var finder = new Mock<IPlayerIdentityFinder>();
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match)).Returns(_playerIdentities);
            var result = new PlayerInMatchStatisticsBuilder(finder.Object).BuildStatisticsForMatch(_matchFixture.Match);

            foreach (var innings in _matchFixture.Match.MatchInnings)
            {
                var fielders = innings.BattingTeam.Team.TeamId == _matchFixture.Match.Teams.Single(x => x.TeamRole == TeamRole.Home).Team.TeamId ? _matchFixture.AwayPlayers : _matchFixture.HomePlayers;

                foreach (var fielder in fielders)
                {
                    var playerRecord = result.SingleOrDefault(x => x.PlayerIdentityId == fielder.PlayerIdentityId && x.MatchInningsId == innings.MatchInningsId);
                    Assert.NotNull(playerRecord);

                    Assert.Equal(innings.PlayerInnings.Count(x => (x.DismissalType == DismissalType.Caught && x.DismissedBy?.PlayerIdentityId == fielder.PlayerIdentityId) ||
                                                                  (x.DismissalType == DismissalType.CaughtAndBowled && x.Bowler?.PlayerIdentityId == fielder.PlayerIdentityId)), playerRecord.Catches);
                    Assert.Equal(innings.PlayerInnings.Count(x => x.DismissalType == DismissalType.RunOut && x.DismissedBy?.PlayerIdentityId == fielder.PlayerIdentityId), playerRecord.RunOuts);
                }
            }
        }

        [Fact]
        public void Batting_team_members_should_not_have_catches_and_run_outs()
        {
            var finder = new Mock<IPlayerIdentityFinder>();
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match)).Returns(_playerIdentities);
            var result = new PlayerInMatchStatisticsBuilder(finder.Object).BuildStatisticsForMatch(_matchFixture.Match);

            foreach (var innings in _matchFixture.Match.MatchInnings)
            {
                var batters = innings.BattingTeam.Team.TeamId == _matchFixture.Match.Teams.Single(x => x.TeamRole == TeamRole.Home).Team.TeamId ? _matchFixture.HomePlayers : _matchFixture.AwayPlayers;

                foreach (var batter in batters)
                {
                    var playerRecords = result.Where(x => x.PlayerIdentityId == batter.PlayerIdentityId && x.MatchInningsId == innings.MatchInningsId);
                    Assert.True(playerRecords.Any());

                    foreach (var playerRecord in playerRecords)
                    {
                        Assert.Null(playerRecord.Catches);
                        Assert.Null(playerRecord.RunOuts);
                    }
                }
            }
        }
    }

    // TODO:

    // Other edge cases?

    // Write tests for other fields not covered

    // playerRecord.OverNumberOfFirstOverBowled
    // playerRecord.BallsBowled
    // playerRecord.OversBowled
    // playerRecord.OversBowledDecimal
    // playerRecord.MaidensBowled
    // playerRecord.RunsConceded
    // playerRecord.HasRunsConceded
    // playerRecord.Wickets
    // playerRecord.WicketsWithBowling
    // playerRecord.WonMatch
    // playerRecord.PlayerOfTheMatch
}
