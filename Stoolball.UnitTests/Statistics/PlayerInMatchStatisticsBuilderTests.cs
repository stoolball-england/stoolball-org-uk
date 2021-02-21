using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using Stoolball.Matches;
using Stoolball.Statistics;
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
                throw new ArgumentNullException(nameof(matchFixture));
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
            if (_matchFixture.Match.InningsOrderIsKnown)
            {
                Assert.Equal(_matchFixture.Match.MatchInnings[0].BattingTeam.MatchTeamId == playerRecord.MatchTeamId, playerRecord.BattedFirst);
            }
            else
            {
                Assert.Null(playerRecord.BattedFirst);
            }
        }

        private static void AssertInningsFields(PlayerIdentity batter, MatchInnings innings, PlayerInMatchStatisticsRecord playerRecord)
        {
            var isOnBattingTeam = innings.BattingTeam.Team.TeamId == batter.Team.TeamId;

            Assert.Equal(innings.InningsPair(), playerRecord.MatchInningsPair);

            Assert.Equal(isOnBattingTeam ? innings.BattingTeam.MatchTeamId : innings.BowlingTeam.MatchTeamId, playerRecord.MatchTeamId);
            Assert.Equal(isOnBattingTeam ? innings.BattingTeam.Team.TeamId : innings.BowlingTeam.Team.TeamId, playerRecord.TeamId);
            Assert.Equal(isOnBattingTeam ? innings.BattingTeam.Team.TeamName : innings.BowlingTeam.Team.TeamName, playerRecord.TeamName);
            Assert.Equal(isOnBattingTeam ? innings.BattingTeam.Team.TeamRoute : innings.BowlingTeam.Team.TeamRoute, playerRecord.TeamRoute);

            Assert.Equal(isOnBattingTeam ? innings.BowlingTeam.Team.TeamId : innings.BattingTeam.Team.TeamId, playerRecord.OppositionTeamId);
            Assert.Equal(isOnBattingTeam ? innings.BowlingTeam.Team.TeamName : innings.BattingTeam.Team.TeamName, playerRecord.OppositionTeamName);
            Assert.Equal(isOnBattingTeam ? innings.BowlingTeam.Team.TeamRoute : innings.BattingTeam.Team.TeamRoute, playerRecord.OppositionTeamRoute);
        }

        [Fact]
        public void Each_batter_should_have_one_record_per_innings_pair_with_player_and_match_data()
        {
            var batters = _matchFixture.Match.MatchInnings.SelectMany(i => i.PlayerInnings.Select(pi => pi.Batter));

            var finder = new Mock<IPlayerIdentityFinder>();
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match)).Returns(_playerIdentities);
            var result = new PlayerInMatchStatisticsBuilder(finder.Object, Mock.Of<IOversHelper>()).BuildStatisticsForMatch(_matchFixture.Match);

            foreach (var batter in batters)
            {
                foreach (var innings in _matchFixture.Match.MatchInnings.Where(x => x.BattingTeam.Team.TeamId == batter.Team.TeamId))
                {
                    var playerRecord = result.SingleOrDefault(x => x.PlayerIdentityId == batter.PlayerIdentityId && x.MatchInningsPair == innings.InningsPair() && (x.PlayerInningsNumber == 1 || x.PlayerInningsNumber == null));
                    Assert.NotNull(playerRecord);

                    AssertPlayerFields(batter, playerRecord);
                    AssertMatchFields(playerRecord);
                }
            }
        }

        [Fact]
        public void Each_batter_should_have_one_record_per_innings_pair_with_innings_data()
        {
            var batters = _matchFixture.Match.MatchInnings.SelectMany(i => i.PlayerInnings.Select(pi => pi.Batter));

            var finder = new Mock<IPlayerIdentityFinder>();
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match)).Returns(_playerIdentities);
            var result = new PlayerInMatchStatisticsBuilder(finder.Object, Mock.Of<IOversHelper>()).BuildStatisticsForMatch(_matchFixture.Match);

            foreach (var identity in batters)
            {
                foreach (var innings in _matchFixture.Match.MatchInnings.Where(x => x.BattingTeam.Team.TeamId == identity.Team.TeamId))
                {
                    var playerRecord = result.SingleOrDefault(x => x.PlayerIdentityId == identity.PlayerIdentityId && x.MatchInningsPair == innings.InningsPair() && (x.PlayerInningsNumber == 1 || x.PlayerInningsNumber == null));
                    Assert.NotNull(playerRecord);

                    AssertInningsFields(identity, innings, playerRecord);

                    var pairedInnings = _matchFixture.Match.MatchInnings.Single(x => x.InningsPair() == innings.InningsPair() && x.MatchInningsId != innings.MatchInningsId);
                    Assert.Equal(innings.Runs, playerRecord.MatchInningsRuns);
                    Assert.Equal(innings.Wickets, playerRecord.OppositionMatchInningsWickets);
                    Assert.Equal(pairedInnings.Runs, playerRecord.OppositionMatchInningsRuns);
                    Assert.Equal(pairedInnings.Wickets, playerRecord.MatchInningsWickets);
                }
            }
        }

        [Fact]
        public void Each_fielder_should_have_one_record_per_innings_pair_with_player_and_match_data()
        {
            var fielders = _matchFixture.Match.MatchInnings.SelectMany(i => i.PlayerInnings.Where(pi => pi.DismissedBy != null).Select(pi => pi.DismissedBy));

            var finder = new Mock<IPlayerIdentityFinder>();
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match)).Returns(_playerIdentities);
            var result = new PlayerInMatchStatisticsBuilder(finder.Object, Mock.Of<IOversHelper>()).BuildStatisticsForMatch(_matchFixture.Match);

            foreach (var identity in fielders)
            {
                foreach (var innings in _matchFixture.Match.MatchInnings.Where(x => x.BowlingTeam.Team.TeamId == identity.Team.TeamId))
                {
                    var playerRecord = result.SingleOrDefault(x => x.PlayerIdentityId == identity.PlayerIdentityId && x.MatchInningsPair == innings.InningsPair() && (x.PlayerInningsNumber == 1 || x.PlayerInningsNumber == null));
                    Assert.NotNull(playerRecord);

                    AssertPlayerFields(identity, playerRecord);
                    AssertMatchFields(playerRecord);
                }
            }
        }

        [Fact]
        public void Each_fielder_should_have_one_record_per_innings_pair_with_innings_data()
        {
            var fielders = _matchFixture.Match.MatchInnings.SelectMany(i => i.PlayerInnings.Where(pi => pi.DismissedBy != null).Select(pi => pi.DismissedBy));

            var finder = new Mock<IPlayerIdentityFinder>();
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match)).Returns(_playerIdentities);
            var result = new PlayerInMatchStatisticsBuilder(finder.Object, Mock.Of<IOversHelper>()).BuildStatisticsForMatch(_matchFixture.Match);

            foreach (var identity in fielders)
            {
                foreach (var innings in _matchFixture.Match.MatchInnings.Where(x => x.BowlingTeam.Team.TeamId == identity.Team.TeamId))
                {
                    var playerRecord = result.SingleOrDefault(x => x.PlayerIdentityId == identity.PlayerIdentityId && x.MatchInningsPair == innings.InningsPair() && (x.PlayerInningsNumber == 1 || x.PlayerInningsNumber == null));
                    Assert.NotNull(playerRecord);

                    AssertInningsFields(identity, innings, playerRecord);

                    var pairedInnings = _matchFixture.Match.MatchInnings.Single(x => x.InningsPair() == innings.InningsPair() && x.MatchInningsId != innings.MatchInningsId);
                    Assert.Equal(pairedInnings.Runs, playerRecord.MatchInningsRuns);
                    Assert.Equal(pairedInnings.Wickets, playerRecord.OppositionMatchInningsWickets);
                    Assert.Equal(innings.Runs, playerRecord.OppositionMatchInningsRuns);
                    Assert.Equal(innings.Wickets, playerRecord.MatchInningsWickets);
                }
            }
        }

        [Fact]
        public void Each_wicket_taker_should_have_one_record_per_innings_pair_with_player_and_match_data()
        {
            var wicketTakers = _matchFixture.Match.MatchInnings.SelectMany(i => i.PlayerInnings.Where(pi => pi.Bowler != null).Select(pi => pi.Bowler));

            var finder = new Mock<IPlayerIdentityFinder>();
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match)).Returns(_playerIdentities);
            var result = new PlayerInMatchStatisticsBuilder(finder.Object, Mock.Of<IOversHelper>()).BuildStatisticsForMatch(_matchFixture.Match);

            foreach (var identity in wicketTakers)
            {
                foreach (var innings in _matchFixture.Match.MatchInnings.Where(x => x.BowlingTeam.Team.TeamId == identity.Team.TeamId))
                {
                    var playerRecord = result.SingleOrDefault(x => x.PlayerIdentityId == identity.PlayerIdentityId && x.MatchInningsPair == innings.InningsPair() && (x.PlayerInningsNumber == 1 || x.PlayerInningsNumber == null));
                    Assert.NotNull(playerRecord);

                    AssertPlayerFields(identity, playerRecord);
                    AssertMatchFields(playerRecord);
                }
            }
        }

        [Fact]
        public void Each_wicket_taker_should_have_one_record_per_innings_pair_with_innings_data()
        {
            var wicketTakers = _matchFixture.Match.MatchInnings.SelectMany(i => i.PlayerInnings.Where(pi => pi.Bowler != null).Select(pi => pi.Bowler));

            var finder = new Mock<IPlayerIdentityFinder>();
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match)).Returns(_playerIdentities);
            var result = new PlayerInMatchStatisticsBuilder(finder.Object, Mock.Of<IOversHelper>()).BuildStatisticsForMatch(_matchFixture.Match);

            foreach (var identity in wicketTakers)
            {
                foreach (var innings in _matchFixture.Match.MatchInnings.Where(x => x.BowlingTeam.Team.TeamId == identity.Team.TeamId))
                {
                    var playerRecord = result.SingleOrDefault(x => x.PlayerIdentityId == identity.PlayerIdentityId && x.MatchInningsPair == innings.InningsPair() && (x.PlayerInningsNumber == 1 || x.PlayerInningsNumber == null));
                    Assert.NotNull(playerRecord);

                    AssertInningsFields(identity, innings, playerRecord);

                    var pairedInnings = _matchFixture.Match.MatchInnings.Single(x => x.InningsPair() == innings.InningsPair() && x.MatchInningsId != innings.MatchInningsId);
                    Assert.Equal(pairedInnings.Runs, playerRecord.MatchInningsRuns);
                    Assert.Equal(pairedInnings.Wickets, playerRecord.OppositionMatchInningsWickets);
                    Assert.Equal(innings.Runs, playerRecord.OppositionMatchInningsRuns);
                    Assert.Equal(innings.Wickets, playerRecord.MatchInningsWickets);
                }
            }
        }

        [Fact]
        public void Each_bowler_with_figures_should_have_one_record_per_innings_pair_with_player_and_match_data()
        {
            var bowlers = _matchFixture.Match.MatchInnings.SelectMany(i => i.BowlingFigures.Select(pi => pi.Bowler));

            var finder = new Mock<IPlayerIdentityFinder>();
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match)).Returns(_playerIdentities);
            var result = new PlayerInMatchStatisticsBuilder(finder.Object, Mock.Of<IOversHelper>()).BuildStatisticsForMatch(_matchFixture.Match);

            foreach (var identity in bowlers)
            {
                foreach (var innings in _matchFixture.Match.MatchInnings.Where(x => x.BowlingTeam.Team.TeamId == identity.Team.TeamId))
                {
                    var playerRecord = result.SingleOrDefault(x => x.PlayerIdentityId == identity.PlayerIdentityId && x.MatchInningsPair == innings.InningsPair() && (x.PlayerInningsNumber == 1 || x.PlayerInningsNumber == null));
                    Assert.NotNull(playerRecord);

                    AssertPlayerFields(identity, playerRecord);
                    AssertMatchFields(playerRecord);
                }
            }
        }

        [Fact]
        public void Each_bowler_with_figures_should_have_one_record_per_innings_pair_with_innings_data()
        {
            var bowlers = _matchFixture.Match.MatchInnings.SelectMany(i => i.BowlingFigures.Select(pi => pi.Bowler));

            var finder = new Mock<IPlayerIdentityFinder>();
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match)).Returns(_playerIdentities);
            var result = new PlayerInMatchStatisticsBuilder(finder.Object, Mock.Of<IOversHelper>()).BuildStatisticsForMatch(_matchFixture.Match);

            foreach (var identity in bowlers)
            {
                foreach (var innings in _matchFixture.Match.MatchInnings.Where(x => x.BowlingTeam.Team.TeamId == identity.Team.TeamId))
                {
                    var playerRecord = result.SingleOrDefault(x => x.PlayerIdentityId == identity.PlayerIdentityId && x.MatchInningsPair == innings.InningsPair() && (x.PlayerInningsNumber == 1 || x.PlayerInningsNumber == null));
                    Assert.NotNull(playerRecord);

                    AssertInningsFields(identity, innings, playerRecord);

                    var pairedInnings = _matchFixture.Match.MatchInnings.Single(x => x.InningsPair() == innings.InningsPair() && x.MatchInningsId != innings.MatchInningsId);
                    Assert.Equal(pairedInnings.Runs, playerRecord.MatchInningsRuns);
                    Assert.Equal(pairedInnings.Wickets, playerRecord.OppositionMatchInningsWickets);
                    Assert.Equal(innings.Runs, playerRecord.OppositionMatchInningsRuns);
                    Assert.Equal(innings.Wickets, playerRecord.MatchInningsWickets);
                }
            }
        }

        [Fact]
        public void Each_bowler_with_overs_should_have_one_record_per_innings_pair_with_player_and_match_data()
        {
            var bowlers = _matchFixture.Match.MatchInnings.SelectMany(i => i.OversBowled.Select(pi => pi.Bowler));

            var finder = new Mock<IPlayerIdentityFinder>();
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match)).Returns(_playerIdentities);
            var result = new PlayerInMatchStatisticsBuilder(finder.Object, Mock.Of<IOversHelper>()).BuildStatisticsForMatch(_matchFixture.Match);

            foreach (var identity in bowlers)
            {
                foreach (var innings in _matchFixture.Match.MatchInnings.Where(x => x.BowlingTeam.Team.TeamId == identity.Team.TeamId))
                {
                    var playerRecord = result.SingleOrDefault(x => x.PlayerIdentityId == identity.PlayerIdentityId && x.MatchInningsPair == innings.InningsPair() && (x.PlayerInningsNumber == 1 || x.PlayerInningsNumber == null));
                    Assert.NotNull(playerRecord);

                    AssertPlayerFields(identity, playerRecord);
                    AssertMatchFields(playerRecord);
                }
            }
        }

        [Fact]
        public void Each_bowler_with_overs_should_have_one_record_per_innings_pair_with_innings_data()
        {
            var bowlers = _matchFixture.Match.MatchInnings.SelectMany(i => i.OversBowled.Select(pi => pi.Bowler));

            var finder = new Mock<IPlayerIdentityFinder>();
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match)).Returns(_playerIdentities);
            var result = new PlayerInMatchStatisticsBuilder(finder.Object, Mock.Of<IOversHelper>()).BuildStatisticsForMatch(_matchFixture.Match);

            foreach (var identity in bowlers)
            {
                foreach (var innings in _matchFixture.Match.MatchInnings.Where(x => x.BowlingTeam.Team.TeamId == identity.Team.TeamId))
                {
                    var playerRecord = result.SingleOrDefault(x => x.PlayerIdentityId == identity.PlayerIdentityId && x.MatchInningsPair == innings.InningsPair() && (x.PlayerInningsNumber == 1 || x.PlayerInningsNumber == null));
                    Assert.NotNull(playerRecord);

                    AssertInningsFields(identity, innings, playerRecord);

                    var pairedInnings = _matchFixture.Match.MatchInnings.Single(x => x.InningsPair() == innings.InningsPair() && x.MatchInningsId != innings.MatchInningsId);
                    Assert.Equal(pairedInnings.Runs, playerRecord.MatchInningsRuns);
                    Assert.Equal(pairedInnings.Wickets, playerRecord.OppositionMatchInningsWickets);
                    Assert.Equal(innings.Runs, playerRecord.OppositionMatchInningsRuns);
                    Assert.Equal(innings.Wickets, playerRecord.MatchInningsWickets);
                }
            }
        }

        [Fact]
        public void Each_award_winner_should_have_one_record_per_innings_pair_with_player_and_match_data()
        {
            var winners = _matchFixture.Match.Awards.Select(pi => pi.PlayerIdentity);

            var finder = new Mock<IPlayerIdentityFinder>();
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match)).Returns(_playerIdentities);
            var result = new PlayerInMatchStatisticsBuilder(finder.Object, Mock.Of<IOversHelper>()).BuildStatisticsForMatch(_matchFixture.Match);

            foreach (var identity in winners)
            {
                foreach (var innings in _matchFixture.Match.MatchInnings.Where(x => x.BattingTeam.Team.TeamId == identity.Team.TeamId))
                {
                    var playerRecord = result.SingleOrDefault(x => x.PlayerIdentityId == identity.PlayerIdentityId && x.MatchInningsPair == innings.InningsPair() && (x.PlayerInningsNumber == 1 || x.PlayerInningsNumber == null));
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
            var result = new PlayerInMatchStatisticsBuilder(finder.Object, Mock.Of<IOversHelper>()).BuildStatisticsForMatch(_matchFixture.Match);

            foreach (var identity in winners)
            {
                foreach (var innings in _matchFixture.Match.MatchInnings.Where(x => x.BattingTeam.Team.TeamId == identity.Team.TeamId))
                {
                    var playerRecord = result.SingleOrDefault(x => x.PlayerIdentityId == identity.PlayerIdentityId && x.MatchInningsPair == innings.InningsPair() && (x.PlayerInningsNumber == 1 || x.PlayerInningsNumber == null));
                    Assert.NotNull(playerRecord);

                    AssertInningsFields(identity, innings, playerRecord);

                    var pairedInnings = _matchFixture.Match.MatchInnings.Single(x => x.InningsPair() == innings.InningsPair() && x.MatchInningsId != innings.MatchInningsId);
                    Assert.Equal(innings.Runs, playerRecord.MatchInningsRuns);
                    Assert.Equal(innings.Wickets, playerRecord.OppositionMatchInningsWickets);
                    Assert.Equal(pairedInnings.Runs, playerRecord.OppositionMatchInningsRuns);
                    Assert.Equal(pairedInnings.Wickets, playerRecord.MatchInningsWickets);
                }
            }
        }

        [Fact]
        public void Only_players_who_bat_again_in_an_innings_should_have_multiple_records_for_an_innings_pair()
        {
            var finder = new Mock<IPlayerIdentityFinder>();
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match)).Returns(_playerIdentities);
            var result = new PlayerInMatchStatisticsBuilder(finder.Object, Mock.Of<IOversHelper>()).BuildStatisticsForMatch(_matchFixture.Match);

            foreach (var identity in _playerIdentities)
            {
                foreach (var innings in _matchFixture.Match.MatchInnings.Where(x => x.BattingTeam.Team.TeamId == identity.Team.TeamId))
                {
                    // In the test data, the first batter bats twice in each innings
                    if (identity.PlayerIdentityId == innings.PlayerInnings[0].Batter.PlayerIdentityId)
                    {
                        Assert.Equal(2, result.Count(x => x.PlayerIdentityId == identity.PlayerIdentityId && x.MatchInningsPair == innings.InningsPair()));
                        Assert.NotNull(result.SingleOrDefault(x => x.PlayerIdentityId == identity.PlayerIdentityId && x.MatchInningsPair == innings.InningsPair() && x.PlayerInningsNumber == 1));
                        Assert.NotNull(result.SingleOrDefault(x => x.PlayerIdentityId == identity.PlayerIdentityId && x.MatchInningsPair == innings.InningsPair() && x.PlayerInningsNumber == 2));
                    }
                    else
                    {
                        Assert.NotNull(result.SingleOrDefault(x => x.PlayerIdentityId == identity.PlayerIdentityId && x.MatchInningsPair == innings.InningsPair()));
                    }
                }
            }
        }

        [Fact]
        public void Each_batter_in_batting_innings_should_have_position_dismissal_and_score()
        {
            var batters = _matchFixture.Match.MatchInnings.SelectMany(i => i.PlayerInnings.Select(pi => pi.Batter));

            var finder = new Mock<IPlayerIdentityFinder>();
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match)).Returns(_playerIdentities);
            var result = new PlayerInMatchStatisticsBuilder(finder.Object, Mock.Of<IOversHelper>()).BuildStatisticsForMatch(_matchFixture.Match);

            foreach (var identity in batters)
            {
                foreach (var innings in _matchFixture.Match.MatchInnings.Where(x => x.BattingTeam.Team.TeamId == identity.Team.TeamId))
                {
                    var expected = innings.PlayerInnings.Where(x => x.Batter.PlayerIdentityId == identity.PlayerIdentityId).ToList();
                    var playerRecords = result.Where(x => x.PlayerIdentityId == identity.PlayerIdentityId && x.MatchInningsPair == innings.InningsPair());

                    Assert.Equal(expected.Count, playerRecords.Count());
                    for (var i = 0; i < expected.Count; i++)
                    {
                        var expectedBattingPosition = expected[i].BattingPosition == 2 ? 1 : expected[i].BattingPosition;
                        var playerRecord = playerRecords.SingleOrDefault(x => x.BattingPosition == expectedBattingPosition);
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
            var batters = _matchFixture.Match.MatchInnings.SelectMany(i => i.PlayerInnings.Select(pi => pi.Batter.PlayerIdentityId.Value)).ToList();
            var missingBatters = _playerIdentities.Where(x => !batters.Contains(x.PlayerIdentityId.Value));

            var finder = new Mock<IPlayerIdentityFinder>();
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match)).Returns(_playerIdentities);
            var result = new PlayerInMatchStatisticsBuilder(finder.Object, Mock.Of<IOversHelper>()).BuildStatisticsForMatch(_matchFixture.Match);

            foreach (var identity in missingBatters)
            {
                foreach (var innings in _matchFixture.Match.MatchInnings.Where(x => x.BattingTeam.Team.TeamId == identity.Team.TeamId))
                {
                    var playerRecord = result.SingleOrDefault(x => x.PlayerIdentityId == identity.PlayerIdentityId && x.MatchInningsPair == innings.InningsPair());
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
        public void Second_batter_in_batting_innings_should_have_position_set_to_1()
        {
            var finder = new Mock<IPlayerIdentityFinder>();
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match)).Returns(_playerIdentities);

            var result = new PlayerInMatchStatisticsBuilder(finder.Object, Mock.Of<IOversHelper>()).BuildStatisticsForMatch(_matchFixture.Match);

            foreach (var innings in _matchFixture.Match.MatchInnings)
            {
                var secondBatter = innings.PlayerInnings.SingleOrDefault(x => x.BattingPosition == 2);
                if (secondBatter == null) { continue; }

                var resultBatter = result.SingleOrDefault(x => x.MatchInningsPair == innings.InningsPair() && x.PlayerIdentityId == secondBatter.Batter.PlayerIdentityId && x.PlayerInningsNumber == 1);
                Assert.Equal(1, resultBatter.BattingPosition);
            }
        }

        [Fact]
        public void Each_dismissal_credited_to_a_bowler_should_have_bowler_details()
        {
            var finder = new Mock<IPlayerIdentityFinder>();
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match)).Returns(_playerIdentities);

            var result = new PlayerInMatchStatisticsBuilder(finder.Object, Mock.Of<IOversHelper>()).BuildStatisticsForMatch(_matchFixture.Match);

            foreach (var innings in _matchFixture.Match.MatchInnings)
            {
                var playerInningsCountForPlayers = new Dictionary<Guid, int>();
                foreach (var playerInnings in innings.PlayerInnings)
                {
                    if (!playerInningsCountForPlayers.ContainsKey(playerInnings.Batter.PlayerIdentityId.Value))
                    {
                        playerInningsCountForPlayers.Add(playerInnings.Batter.PlayerIdentityId.Value, 1);
                    }
                    else
                    {
                        playerInningsCountForPlayers[playerInnings.Batter.PlayerIdentityId.Value]++;
                    }

                    var playerRecord = result.SingleOrDefault(x => x.PlayerIdentityId == playerInnings.Batter.PlayerIdentityId && x.MatchInningsPair == innings.InningsPair() && x.PlayerInningsNumber == playerInningsCountForPlayers[playerInnings.Batter.PlayerIdentityId.Value]);
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

            var result = new PlayerInMatchStatisticsBuilder(finder.Object, Mock.Of<IOversHelper>()).BuildStatisticsForMatch(_matchFixture.Match);

            foreach (var innings in _matchFixture.Match.MatchInnings)
            {
                var playerInningsCountForPlayers = new Dictionary<Guid, int>();
                foreach (var playerInnings in innings.PlayerInnings)
                {
                    if (!playerInningsCountForPlayers.ContainsKey(playerInnings.Batter.PlayerIdentityId.Value))
                    {
                        playerInningsCountForPlayers.Add(playerInnings.Batter.PlayerIdentityId.Value, 1);
                    }
                    else
                    {
                        playerInningsCountForPlayers[playerInnings.Batter.PlayerIdentityId.Value]++;
                    }

                    var playerRecord = result.SingleOrDefault(x => x.PlayerIdentityId == playerInnings.Batter.PlayerIdentityId && x.MatchInningsPair == innings.InningsPair() && x.PlayerInningsNumber == playerInningsCountForPlayers[playerInnings.Batter.PlayerIdentityId.Value]);
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

            var result = new PlayerInMatchStatisticsBuilder(finder.Object, Mock.Of<IOversHelper>()).BuildStatisticsForMatch(_matchFixture.Match);

            foreach (var innings in _matchFixture.Match.MatchInnings)
            {
                var playerInningsCountForPlayers = new Dictionary<Guid, int>();
                foreach (var playerInnings in innings.PlayerInnings)
                {
                    if (!playerInningsCountForPlayers.ContainsKey(playerInnings.Batter.PlayerIdentityId.Value))
                    {
                        playerInningsCountForPlayers.Add(playerInnings.Batter.PlayerIdentityId.Value, 1);
                    }
                    else
                    {
                        playerInningsCountForPlayers[playerInnings.Batter.PlayerIdentityId.Value]++;
                    }

                    var playerRecord = result.SingleOrDefault(x => x.PlayerIdentityId == playerInnings.Batter.PlayerIdentityId && x.MatchInningsPair == innings.InningsPair() && x.PlayerInningsNumber == playerInningsCountForPlayers[playerInnings.Batter.PlayerIdentityId.Value]);
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
        public void Bowlers_should_have_first_over_from_overs_data()
        {
            var finder = new Mock<IPlayerIdentityFinder>();
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match)).Returns(_playerIdentities);

            var result = new PlayerInMatchStatisticsBuilder(finder.Object, Mock.Of<IOversHelper>()).BuildStatisticsForMatch(_matchFixture.Match);

            foreach (var innings in _matchFixture.Match.MatchInnings)
            {
                var bowlersWithOvers = innings.OversBowled.OrderBy(x => x.OverNumber).Select(x => x.Bowler.PlayerIdentityId).Distinct();

                foreach (var bowler in bowlersWithOvers)
                {
                    var playerRecord = result.SingleOrDefault(x => x.PlayerIdentityId == bowler && x.MatchInningsPair == innings.InningsPair() && (x.PlayerInningsNumber == 1 || x.PlayerInningsNumber == null));
                    Assert.NotNull(playerRecord);

                    Assert.Equal(innings.OversBowled.First(x => x.Bowler.PlayerIdentityId == bowler).OverNumber, playerRecord.OverNumberOfFirstOverBowled);
                }
            }
        }

        [Fact]
        public void Bowlers_should_have_figures_from_bowling_figures_data()
        {
            var finder = new Mock<IPlayerIdentityFinder>();
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match)).Returns(_playerIdentities);

            var result = new PlayerInMatchStatisticsBuilder(finder.Object, Mock.Of<IOversHelper>()).BuildStatisticsForMatch(_matchFixture.Match);

            foreach (var innings in _matchFixture.Match.MatchInnings)
            {
                foreach (var figures in innings.BowlingFigures)
                {
                    var playerRecord = result.SingleOrDefault(x => x.PlayerIdentityId == figures.Bowler.PlayerIdentityId && x.MatchInningsPair == innings.InningsPair() && (x.PlayerInningsNumber == 1 || x.PlayerInningsNumber == null));
                    Assert.NotNull(playerRecord);

                    Assert.Equal(figures.Overs, playerRecord.Overs);
                    Assert.Equal(figures.Maidens, playerRecord.Maidens);
                    Assert.Equal(figures.RunsConceded, playerRecord.RunsConceded);
                    Assert.Equal(figures.RunsConceded.HasValue, playerRecord.HasRunsConceded);
                    Assert.Equal(figures.Wickets, playerRecord.Wickets);
                }
            }
        }

        [Fact]
        public void Players_without_bowling_figures_should_have_null_data()
        {
            var finder = new Mock<IPlayerIdentityFinder>();
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match)).Returns(_playerIdentities);

            var result = new PlayerInMatchStatisticsBuilder(finder.Object, Mock.Of<IOversHelper>()).BuildStatisticsForMatch(_matchFixture.Match);

            foreach (var innings in _matchFixture.Match.MatchInnings)
            {
                var pairedInnings = _matchFixture.Match.MatchInnings.Single(x => x.InningsPair() == innings.InningsPair() && x.MatchInningsId != innings.MatchInningsId);

                foreach (var identity in _playerIdentities.Where(x => !innings.BowlingFigures.Select(bf => bf.Bowler.PlayerIdentityId).Contains(x.PlayerIdentityId) &&
                                                                      !pairedInnings.BowlingFigures.Select(bf => bf.Bowler.PlayerIdentityId).Contains(x.PlayerIdentityId)))
                {
                    var playerRecords = result.Where(x => x.PlayerIdentityId == identity.PlayerIdentityId && x.MatchInningsPair == innings.InningsPair() && (x.PlayerInningsNumber == 1 || x.PlayerInningsNumber == null));

                    foreach (var playerRecord in playerRecords)
                    {
                        Assert.Null(playerRecord.Overs);
                        Assert.Null(playerRecord.Maidens);
                        Assert.Null(playerRecord.RunsConceded);
                        Assert.False(playerRecord.HasRunsConceded);
                        Assert.Null(playerRecord.Wickets);
                    }
                }
            }
        }

        [Fact]
        public void Bowlers_with_bowling_figures_have_wickets_with_bowling_if_they_have_overs_data()
        {
            var finder = new Mock<IPlayerIdentityFinder>();
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match)).Returns(_playerIdentities);

            var result = new PlayerInMatchStatisticsBuilder(finder.Object, Mock.Of<IOversHelper>()).BuildStatisticsForMatch(_matchFixture.Match);

            foreach (var innings in _matchFixture.Match.MatchInnings)
            {
                foreach (var figures in innings.BowlingFigures)
                {
                    var bowlerHasOversData = innings.OversBowled.Any(x => x.Bowler.PlayerIdentityId == figures.Bowler.PlayerIdentityId);

                    var playerRecord = result.SingleOrDefault(x => x.PlayerIdentityId == figures.Bowler.PlayerIdentityId && x.MatchInningsPair == innings.InningsPair() && (x.PlayerInningsNumber == 1 || x.PlayerInningsNumber == null));
                    Assert.NotNull(playerRecord);

                    if (bowlerHasOversData)
                    {
                        Assert.Equal(playerRecord.Wickets, playerRecord.WicketsWithBowling);
                    }
                    else
                    {
                        Assert.Null(playerRecord.WicketsWithBowling);
                    }
                }
            }
        }

        [Fact]
        public void Bowlers_have_balls_bowled_from_overs_data_if_available_or_bowling_figures_if_not()
        {
            const int MOCK_RESULT_OF_OVERS_TO_BALLS_BOWLED = 10000;
            var oversHelper = new Mock<IOversHelper>();
            oversHelper.Setup(x => x.OversToBallsBowled(It.IsAny<decimal>())).Returns(MOCK_RESULT_OF_OVERS_TO_BALLS_BOWLED);

            var finder = new Mock<IPlayerIdentityFinder>();
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match)).Returns(_playerIdentities);

            var result = new PlayerInMatchStatisticsBuilder(finder.Object, oversHelper.Object).BuildStatisticsForMatch(_matchFixture.Match);

            foreach (var innings in _matchFixture.Match.MatchInnings)
            {
                foreach (var figures in innings.BowlingFigures)
                {
                    var bowlerOversData = innings.OversBowled.Where(x => x.Bowler.PlayerIdentityId == figures.Bowler.PlayerIdentityId);

                    var playerRecord = result.SingleOrDefault(x => x.PlayerIdentityId == figures.Bowler.PlayerIdentityId && x.MatchInningsPair == innings.InningsPair() && (x.PlayerInningsNumber == 1 || x.PlayerInningsNumber == null));
                    Assert.NotNull(playerRecord);

                    if (bowlerOversData.Any())
                    {
                        Assert.Equal(bowlerOversData.Sum(x => x.BallsBowled), playerRecord.BallsBowled);
                    }
                    else if (figures.Overs.HasValue)
                    {
                        Assert.Equal(MOCK_RESULT_OF_OVERS_TO_BALLS_BOWLED, playerRecord.BallsBowled);
                    }
                    else
                    {
                        Assert.Null(playerRecord.BallsBowled);
                    }
                }
            }
        }

        [Fact]
        public void All_players_should_have_catches_and_run_outs()
        {
            var finder = new Mock<IPlayerIdentityFinder>();
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match)).Returns(_playerIdentities);

            var result = new PlayerInMatchStatisticsBuilder(finder.Object, Mock.Of<IOversHelper>()).BuildStatisticsForMatch(_matchFixture.Match);

            foreach (var identity in _playerIdentities)
            {
                foreach (var innings in _matchFixture.Match.MatchInnings.Where(x => x.BattingTeam.Team.TeamId == identity.Team.TeamId))
                {
                    var pairedInnings = _matchFixture.Match.MatchInnings.Single(x => x.InningsPair() == innings.InningsPair() && x.MatchInningsId != innings.MatchInningsId);

                    var playerRecord = result.SingleOrDefault(x => x.PlayerIdentityId == identity.PlayerIdentityId && x.MatchInningsPair == innings.InningsPair() && (x.PlayerInningsNumber == 1 || x.PlayerInningsNumber == null));
                    Assert.NotNull(playerRecord);

                    Assert.Equal(pairedInnings.PlayerInnings.Count(x => (x.DismissalType == DismissalType.Caught && x.DismissedBy?.PlayerIdentityId == identity.PlayerIdentityId) ||
                                                                  (x.DismissalType == DismissalType.CaughtAndBowled && x.Bowler?.PlayerIdentityId == identity.PlayerIdentityId)), playerRecord.Catches);
                    Assert.Equal(pairedInnings.PlayerInnings.Count(x => x.DismissalType == DismissalType.RunOut && x.DismissedBy?.PlayerIdentityId == identity.PlayerIdentityId), playerRecord.RunOuts);
                }
            }
        }

        [Fact]
        public void Every_record_should_record_whether_the_match_was_won()
        {
            var finder = new Mock<IPlayerIdentityFinder>();
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match)).Returns(_playerIdentities);

            var result = new PlayerInMatchStatisticsBuilder(finder.Object, Mock.Of<IOversHelper>()).BuildStatisticsForMatch(_matchFixture.Match);

            foreach (var identity in _playerIdentities)
            {
                foreach (var innings in _matchFixture.Match.MatchInnings.Where(x => x.BattingTeam.Team.TeamId == identity.Team.TeamId))
                {
                    var playerRecords = result.Where(x => x.PlayerIdentityId == identity.PlayerIdentityId && x.MatchInningsPair == innings.InningsPair());
                    Assert.True(playerRecords.Any());

                    foreach (var playerRecord in playerRecords)
                    {
                        var isHomePlayer = identity.Team.TeamId == _matchFixture.Match.Teams.Single(x => x.TeamRole == TeamRole.Home).Team.TeamId;
                        if (_matchFixture.Match.MatchResultType == MatchResultType.HomeWin)
                        {
                            Assert.Equal(isHomePlayer ? 1 : -1, playerRecord.WonMatch);
                        }
                        else if (_matchFixture.Match.MatchResultType == MatchResultType.Tie)
                        {
                            Assert.Equal(0, playerRecord.WonMatch);
                        }
                        else if (_matchFixture.Match.MatchResultType == MatchResultType.AwayWin)
                        {
                            Assert.Equal(isHomePlayer ? -1 : 1, playerRecord.WonMatch);
                        }
                        else
                        {
                            Assert.Null(playerRecord.WonMatch);
                        }
                    }
                }
            }
        }

        [Fact]
        public void Every_record_should_record_whether_the_player_was_player_of_the_match()
        {
            var finder = new Mock<IPlayerIdentityFinder>();
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match)).Returns(_playerIdentities);

            var result = new PlayerInMatchStatisticsBuilder(finder.Object, Mock.Of<IOversHelper>()).BuildStatisticsForMatch(_matchFixture.Match);

            foreach (var identity in _playerIdentities)
            {
                foreach (var innings in _matchFixture.Match.MatchInnings.Where(x => x.BattingTeam.Team.TeamId == identity.Team.TeamId))
                {
                    var playerRecords = result.Where(x => x.PlayerIdentityId == identity.PlayerIdentityId && x.MatchInningsPair == innings.InningsPair());
                    Assert.True(playerRecords.Any());

                    foreach (var playerRecord in playerRecords)
                    {
                        Assert.Equal(_matchFixture.Match.Awards.Any(x => x.Award.AwardName.ToUpperInvariant() == StatisticsConstants.PLAYER_OF_THE_MATCH_AWARD && x.PlayerIdentity.PlayerIdentityId == identity.PlayerIdentityId), playerRecord.PlayerOfTheMatch);
                    }
                }
            }
        }
    }
}
