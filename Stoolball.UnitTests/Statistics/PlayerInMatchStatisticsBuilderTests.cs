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
        }

        private void AssertMatchFields(PlayerInMatchStatisticsRecord playerRecord)
        {
            Assert.Equal(_matchFixture.Match.MatchId, playerRecord.MatchId);
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
            Assert.Equal(isOnBattingTeam ? innings.BowlingTeam.Team.TeamId : innings.BattingTeam.Team.TeamId, playerRecord.OppositionTeamId);
        }

        private static void AssertTeamTotals(MatchInnings battingInnings, MatchInnings bowlingInnings, PlayerInMatchStatisticsRecord playerRecord)
        {
            Assert.Equal(battingInnings.Runs, playerRecord.TeamRunsScored);
            Assert.Equal(battingInnings.BonusOrPenaltyRuns, playerRecord.TeamBonusOrPenaltyRunsAwarded);
            Assert.Equal(battingInnings.Wickets, playerRecord.TeamWicketsLost);
            Assert.Equal(bowlingInnings.Runs, playerRecord.TeamRunsConceded);
            Assert.Equal(bowlingInnings.NoBalls, playerRecord.TeamNoBallsConceded);
            Assert.Equal(bowlingInnings.Wides, playerRecord.TeamWidesConceded);
            Assert.Equal(bowlingInnings.Byes, playerRecord.TeamByesConceded);
            Assert.Equal(bowlingInnings.Wickets, playerRecord.TeamWicketsTaken);
        }

        [Fact]
        public void Each_batter_should_have_one_record_per_innings_pair_with_player_and_match_data()
        {
            var batters = _matchFixture.Match.MatchInnings.SelectMany(i => i.PlayerInnings.Select(pi => pi.Batter));

            var finder = new Mock<IPlayerIdentityFinder>();
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match, TeamRole.Home)).Returns(_matchFixture.HomePlayers);
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match, TeamRole.Away)).Returns(_matchFixture.AwayPlayers);
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
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match, TeamRole.Home)).Returns(_matchFixture.HomePlayers);
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match, TeamRole.Away)).Returns(_matchFixture.AwayPlayers);
            var result = new PlayerInMatchStatisticsBuilder(finder.Object, Mock.Of<IOversHelper>()).BuildStatisticsForMatch(_matchFixture.Match);

            foreach (var identity in batters)
            {
                foreach (var innings in _matchFixture.Match.MatchInnings.Where(x => x.BattingTeam.Team.TeamId == identity.Team.TeamId))
                {
                    var playerRecord = result.SingleOrDefault(x => x.PlayerIdentityId == identity.PlayerIdentityId && x.MatchInningsPair == innings.InningsPair() && (x.PlayerInningsNumber == 1 || x.PlayerInningsNumber == null));
                    Assert.NotNull(playerRecord);

                    AssertInningsFields(identity, innings, playerRecord);

                    var pairedInnings = _matchFixture.Match.MatchInnings.Single(x => x.InningsPair() == innings.InningsPair() && x.MatchInningsId != innings.MatchInningsId);
                    AssertTeamTotals(innings, pairedInnings, playerRecord);
                }
            }
        }

        [Fact]
        public void Each_fielder_should_have_one_record_per_innings_pair_with_player_and_match_data()
        {
            var fielders = _matchFixture.Match.MatchInnings.SelectMany(i => i.PlayerInnings.Where(pi => pi.DismissedBy != null).Select(pi => pi.DismissedBy));

            var finder = new Mock<IPlayerIdentityFinder>();
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match, TeamRole.Home)).Returns(_matchFixture.HomePlayers);
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match, TeamRole.Away)).Returns(_matchFixture.AwayPlayers);
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
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match, TeamRole.Home)).Returns(_matchFixture.HomePlayers);
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match, TeamRole.Away)).Returns(_matchFixture.AwayPlayers);
            var result = new PlayerInMatchStatisticsBuilder(finder.Object, Mock.Of<IOversHelper>()).BuildStatisticsForMatch(_matchFixture.Match);

            foreach (var identity in fielders)
            {
                foreach (var innings in _matchFixture.Match.MatchInnings.Where(x => x.BowlingTeam.Team.TeamId == identity.Team.TeamId))
                {
                    var playerRecord = result.SingleOrDefault(x => x.PlayerIdentityId == identity.PlayerIdentityId && x.MatchInningsPair == innings.InningsPair() && (x.PlayerInningsNumber == 1 || x.PlayerInningsNumber == null));
                    Assert.NotNull(playerRecord);

                    AssertInningsFields(identity, innings, playerRecord);

                    var pairedInnings = _matchFixture.Match.MatchInnings.Single(x => x.InningsPair() == innings.InningsPair() && x.MatchInningsId != innings.MatchInningsId);
                    AssertTeamTotals(pairedInnings, innings, playerRecord);
                }
            }
        }

        [Fact]
        public void Each_wicket_taker_should_have_one_record_per_innings_pair_with_player_and_match_data()
        {
            var wicketTakers = _matchFixture.Match.MatchInnings.SelectMany(i => i.PlayerInnings.Where(pi => pi.Bowler != null).Select(pi => pi.Bowler));

            var finder = new Mock<IPlayerIdentityFinder>();
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match, TeamRole.Home)).Returns(_matchFixture.HomePlayers);
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match, TeamRole.Away)).Returns(_matchFixture.AwayPlayers);
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
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match, TeamRole.Home)).Returns(_matchFixture.HomePlayers);
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match, TeamRole.Away)).Returns(_matchFixture.AwayPlayers);
            var result = new PlayerInMatchStatisticsBuilder(finder.Object, Mock.Of<IOversHelper>()).BuildStatisticsForMatch(_matchFixture.Match);

            foreach (var identity in wicketTakers)
            {
                foreach (var innings in _matchFixture.Match.MatchInnings.Where(x => x.BowlingTeam.Team.TeamId == identity.Team.TeamId))
                {
                    var playerRecord = result.SingleOrDefault(x => x.PlayerIdentityId == identity.PlayerIdentityId && x.MatchInningsPair == innings.InningsPair() && (x.PlayerInningsNumber == 1 || x.PlayerInningsNumber == null));
                    Assert.NotNull(playerRecord);

                    AssertInningsFields(identity, innings, playerRecord);

                    var pairedInnings = _matchFixture.Match.MatchInnings.Single(x => x.InningsPair() == innings.InningsPair() && x.MatchInningsId != innings.MatchInningsId);
                    AssertTeamTotals(pairedInnings, innings, playerRecord);
                }
            }
        }

        [Fact]
        public void Each_bowler_with_figures_should_have_one_record_per_innings_pair_with_player_and_match_data()
        {
            var bowlers = _matchFixture.Match.MatchInnings.SelectMany(i => i.BowlingFigures.Select(pi => pi.Bowler));

            var finder = new Mock<IPlayerIdentityFinder>();
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match, TeamRole.Home)).Returns(_matchFixture.HomePlayers);
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match, TeamRole.Away)).Returns(_matchFixture.AwayPlayers);
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
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match, TeamRole.Home)).Returns(_matchFixture.HomePlayers);
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match, TeamRole.Away)).Returns(_matchFixture.AwayPlayers);
            var result = new PlayerInMatchStatisticsBuilder(finder.Object, Mock.Of<IOversHelper>()).BuildStatisticsForMatch(_matchFixture.Match);

            foreach (var identity in bowlers)
            {
                foreach (var innings in _matchFixture.Match.MatchInnings.Where(x => x.BowlingTeam.Team.TeamId == identity.Team.TeamId))
                {
                    var playerRecord = result.SingleOrDefault(x => x.PlayerIdentityId == identity.PlayerIdentityId && x.MatchInningsPair == innings.InningsPair() && (x.PlayerInningsNumber == 1 || x.PlayerInningsNumber == null));
                    Assert.NotNull(playerRecord);

                    AssertInningsFields(identity, innings, playerRecord);

                    var pairedInnings = _matchFixture.Match.MatchInnings.Single(x => x.InningsPair() == innings.InningsPair() && x.MatchInningsId != innings.MatchInningsId);
                    AssertTeamTotals(pairedInnings, innings, playerRecord);
                }
            }
        }

        [Fact]
        public void Each_bowler_with_overs_should_have_one_record_per_innings_pair_with_player_and_match_data()
        {
            var bowlers = _matchFixture.Match.MatchInnings.SelectMany(i => i.OversBowled.Select(pi => pi.Bowler));

            var finder = new Mock<IPlayerIdentityFinder>();
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match, TeamRole.Home)).Returns(_matchFixture.HomePlayers);
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match, TeamRole.Away)).Returns(_matchFixture.AwayPlayers);
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
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match, TeamRole.Home)).Returns(_matchFixture.HomePlayers);
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match, TeamRole.Away)).Returns(_matchFixture.AwayPlayers);
            var result = new PlayerInMatchStatisticsBuilder(finder.Object, Mock.Of<IOversHelper>()).BuildStatisticsForMatch(_matchFixture.Match);

            foreach (var identity in bowlers)
            {
                foreach (var innings in _matchFixture.Match.MatchInnings.Where(x => x.BowlingTeam.Team.TeamId == identity.Team.TeamId))
                {
                    var playerRecord = result.SingleOrDefault(x => x.PlayerIdentityId == identity.PlayerIdentityId && x.MatchInningsPair == innings.InningsPair() && (x.PlayerInningsNumber == 1 || x.PlayerInningsNumber == null));
                    Assert.NotNull(playerRecord);

                    AssertInningsFields(identity, innings, playerRecord);

                    var pairedInnings = _matchFixture.Match.MatchInnings.Single(x => x.InningsPair() == innings.InningsPair() && x.MatchInningsId != innings.MatchInningsId);
                    AssertTeamTotals(pairedInnings, innings, playerRecord);
                }
            }
        }

        [Fact]
        public void Each_award_winner_should_have_one_record_per_innings_pair_with_player_and_match_data()
        {
            var winners = _matchFixture.Match.Awards.Select(pi => pi.PlayerIdentity);

            var finder = new Mock<IPlayerIdentityFinder>();
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match, TeamRole.Home)).Returns(_matchFixture.HomePlayers);
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match, TeamRole.Away)).Returns(_matchFixture.AwayPlayers);
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
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match, TeamRole.Home)).Returns(_matchFixture.HomePlayers);
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match, TeamRole.Away)).Returns(_matchFixture.AwayPlayers);
            var result = new PlayerInMatchStatisticsBuilder(finder.Object, Mock.Of<IOversHelper>()).BuildStatisticsForMatch(_matchFixture.Match);

            foreach (var identity in winners)
            {
                foreach (var innings in _matchFixture.Match.MatchInnings.Where(x => x.BattingTeam.Team.TeamId == identity.Team.TeamId))
                {
                    var playerRecord = result.SingleOrDefault(x => x.PlayerIdentityId == identity.PlayerIdentityId && x.MatchInningsPair == innings.InningsPair() && (x.PlayerInningsNumber == 1 || x.PlayerInningsNumber == null));
                    Assert.NotNull(playerRecord);

                    AssertInningsFields(identity, innings, playerRecord);

                    var pairedInnings = _matchFixture.Match.MatchInnings.Single(x => x.InningsPair() == innings.InningsPair() && x.MatchInningsId != innings.MatchInningsId);
                    AssertTeamTotals(innings, pairedInnings, playerRecord);
                }
            }
        }

        [Fact]
        public void Only_players_who_bat_again_in_an_innings_should_have_multiple_records_for_an_innings_pair()
        {
            var finder = new Mock<IPlayerIdentityFinder>();
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match, TeamRole.Home)).Returns(_matchFixture.HomePlayers);
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match, TeamRole.Away)).Returns(_matchFixture.AwayPlayers);
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
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match, TeamRole.Home)).Returns(_matchFixture.HomePlayers);
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match, TeamRole.Away)).Returns(_matchFixture.AwayPlayers);
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
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match, TeamRole.Home)).Returns(_matchFixture.HomePlayers);
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match, TeamRole.Away)).Returns(_matchFixture.AwayPlayers);
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
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match, TeamRole.Home)).Returns(_matchFixture.HomePlayers);
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match, TeamRole.Away)).Returns(_matchFixture.AwayPlayers);

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
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match, TeamRole.Home)).Returns(_matchFixture.HomePlayers);
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match, TeamRole.Away)).Returns(_matchFixture.AwayPlayers);

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
                    }
                    else
                    {
                        Assert.Null(playerRecord.BowledByPlayerIdentityId);
                    }
                }
            }
        }

        [Fact]
        public void Each_caught_dismissal_should_have_catcher_details()
        {
            var finder = new Mock<IPlayerIdentityFinder>();
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match, TeamRole.Home)).Returns(_matchFixture.HomePlayers);
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match, TeamRole.Away)).Returns(_matchFixture.AwayPlayers);

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
                    }
                    else if (playerInnings.DismissalType == DismissalType.CaughtAndBowled && playerInnings.Bowler != null)
                    {
                        Assert.Equal(playerInnings.Bowler.PlayerIdentityId, playerRecord.CaughtByPlayerIdentityId);
                    }
                    else
                    {
                        Assert.Null(playerRecord.CaughtByPlayerIdentityId);
                    }
                }
            }
        }

        [Fact]
        public void Each_run_out_dismissal_should_have_fielder_details()
        {
            var finder = new Mock<IPlayerIdentityFinder>();
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match, TeamRole.Home)).Returns(_matchFixture.HomePlayers);
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match, TeamRole.Away)).Returns(_matchFixture.AwayPlayers);

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
                    }
                    else
                    {
                        Assert.Null(playerRecord.RunOutByPlayerIdentityId);
                    }
                }
            }
        }

        [Fact]
        public void Bowlers_should_have_first_over_from_overs_data()
        {
            var finder = new Mock<IPlayerIdentityFinder>();
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match, TeamRole.Home)).Returns(_matchFixture.HomePlayers);
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match, TeamRole.Away)).Returns(_matchFixture.AwayPlayers);

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
        public void Bowlers_should_have_no_balls_and_wides_from_overs_data()
        {
            var finder = new Mock<IPlayerIdentityFinder>();
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match, TeamRole.Home)).Returns(_matchFixture.HomePlayers);
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match, TeamRole.Away)).Returns(_matchFixture.AwayPlayers);

            var result = new PlayerInMatchStatisticsBuilder(finder.Object, Mock.Of<IOversHelper>()).BuildStatisticsForMatch(_matchFixture.Match);

            foreach (var innings in _matchFixture.Match.MatchInnings)
            {
                var bowlersWithOvers = innings.OversBowled.OrderBy(x => x.OverNumber).Select(x => x.Bowler.PlayerIdentityId).Distinct();

                foreach (var bowler in bowlersWithOvers)
                {
                    var playerRecord = result.SingleOrDefault(x => x.PlayerIdentityId == bowler && x.MatchInningsPair == innings.InningsPair() && (x.PlayerInningsNumber == 1 || x.PlayerInningsNumber == null));
                    Assert.NotNull(playerRecord);

                    Assert.Equal(innings.OversBowled.Where(x => x.Bowler.PlayerIdentityId == bowler).Sum(x => x.NoBalls), playerRecord.NoBalls);
                    Assert.Equal(innings.OversBowled.Where(x => x.Bowler.PlayerIdentityId == bowler).Sum(x => x.Wides), playerRecord.Wides);
                }
            }
        }

        [Fact]
        public void Bowlers_should_have_figures_from_bowling_figures_data()
        {
            var finder = new Mock<IPlayerIdentityFinder>();
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match, TeamRole.Home)).Returns(_matchFixture.HomePlayers);
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match, TeamRole.Away)).Returns(_matchFixture.AwayPlayers);

            var result = new PlayerInMatchStatisticsBuilder(finder.Object, Mock.Of<IOversHelper>()).BuildStatisticsForMatch(_matchFixture.Match);

            foreach (var innings in _matchFixture.Match.MatchInnings)
            {
                foreach (var figures in innings.BowlingFigures)
                {
                    var playerRecord = result.SingleOrDefault(x => x.PlayerIdentityId == figures.Bowler.PlayerIdentityId && x.MatchInningsPair == innings.InningsPair() && (x.PlayerInningsNumber == 1 || x.PlayerInningsNumber == null));
                    Assert.NotNull(playerRecord);

                    Assert.Equal(figures.BowlingFiguresId, playerRecord.BowlingFiguresId);
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
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match, TeamRole.Home)).Returns(_matchFixture.HomePlayers);
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match, TeamRole.Away)).Returns(_matchFixture.AwayPlayers);

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
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match, TeamRole.Home)).Returns(_matchFixture.HomePlayers);
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match, TeamRole.Away)).Returns(_matchFixture.AwayPlayers);

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
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match, TeamRole.Home)).Returns(_matchFixture.HomePlayers);
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match, TeamRole.Away)).Returns(_matchFixture.AwayPlayers);

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
                        Assert.Equal(bowlerOversData.Where(x => x.BallsBowled.HasValue).Sum(x => x.BallsBowled) + bowlerOversData.Count(x => !x.BallsBowled.HasValue) * StatisticsConstants.BALLS_PER_OVER, playerRecord.BallsBowled);
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
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match, TeamRole.Home)).Returns(_matchFixture.HomePlayers);
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match, TeamRole.Away)).Returns(_matchFixture.AwayPlayers);

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
        public void Every_record_should_record_whether_the_toss_was_won()
        {
            var finder = new Mock<IPlayerIdentityFinder>();
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match, TeamRole.Home)).Returns(_matchFixture.HomePlayers);
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match, TeamRole.Away)).Returns(_matchFixture.AwayPlayers);

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
                        var teamInMatch = _matchFixture.Match.Teams.Single(x => x.TeamRole == (isHomePlayer ? TeamRole.Home : TeamRole.Away));
                        Assert.Equal(teamInMatch.WonToss, playerRecord.WonToss);
                    }
                }
            }
        }

        [Fact]
        public void Every_record_should_record_whether_the_match_was_won()
        {
            var finder = new Mock<IPlayerIdentityFinder>();
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match, TeamRole.Home)).Returns(_matchFixture.HomePlayers);
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match, TeamRole.Away)).Returns(_matchFixture.AwayPlayers);

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
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match, TeamRole.Home)).Returns(_matchFixture.HomePlayers);
            finder.Setup(x => x.PlayerIdentitiesInMatch(_matchFixture.Match, TeamRole.Away)).Returns(_matchFixture.AwayPlayers);

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
