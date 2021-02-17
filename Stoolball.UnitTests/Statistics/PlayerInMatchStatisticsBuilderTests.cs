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
        private readonly Stoolball.Matches.Match _match;
        private List<PlayerIdentity> _playerIdentities = new List<PlayerIdentity>();

        public PlayerInMatchStatisticsBuilderTests(MatchFixture matchFixture)
        {
            if (matchFixture is null || matchFixture.Match is null)
            {
                throw new System.ArgumentNullException(nameof(matchFixture));
            }

            _match = matchFixture.Match;
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
            Assert.Equal(_match.MatchId, playerRecord.MatchId);
            Assert.Equal(_match.MatchName, playerRecord.MatchName);
            Assert.Equal(_match.MatchType, playerRecord.MatchType);
            Assert.Equal(_match.PlayerType, playerRecord.MatchPlayerType);
            Assert.Equal(_match.MatchRoute, playerRecord.MatchRoute);
            Assert.Equal(_match.StartTime, playerRecord.MatchStartTime);
            Assert.Equal(_match.Tournament?.TournamentId, playerRecord.TournamentId);
            Assert.Equal(_match.MatchLocation.MatchLocationId, playerRecord.MatchLocationId);
            Assert.Equal(_match.Season.SeasonId, playerRecord.SeasonId);
            Assert.Equal(_match.Season.Competition.CompetitionId, playerRecord.CompetitionId);
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
            Assert.Equal(_match.InningsOrderIsKnown, playerRecord.InningsOrderIsKnown);
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
            var batters = _match.MatchInnings.SelectMany(i => i.PlayerInnings.Select(pi => pi.PlayerIdentity));

            var finder = new Mock<IPlayerIdentityFinder>();
            finder.Setup(x => x.PlayerIdentitiesInMatch(_match)).Returns(_playerIdentities);
            var result = new PlayerInMatchStatisticsBuilder(finder.Object).BuildStatisticsForMatch(_match);

            foreach (var batter in batters)
            {
                foreach (var innings in _match.MatchInnings)
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
            var batters = _match.MatchInnings.SelectMany(i => i.PlayerInnings.Select(pi => pi.PlayerIdentity));

            var finder = new Mock<IPlayerIdentityFinder>();
            finder.Setup(x => x.PlayerIdentitiesInMatch(_match)).Returns(_playerIdentities);
            var result = new PlayerInMatchStatisticsBuilder(finder.Object).BuildStatisticsForMatch(_match);

            foreach (var batter in batters)
            {
                foreach (var innings in _match.MatchInnings)
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
            var fielders = _match.MatchInnings.SelectMany(i => i.PlayerInnings.Where(pi => pi.DismissedBy != null).Select(pi => pi.DismissedBy));

            var finder = new Mock<IPlayerIdentityFinder>();
            finder.Setup(x => x.PlayerIdentitiesInMatch(_match)).Returns(_playerIdentities);
            var result = new PlayerInMatchStatisticsBuilder(finder.Object).BuildStatisticsForMatch(_match);

            foreach (var identity in fielders)
            {
                foreach (var innings in _match.MatchInnings)
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
            var fielders = _match.MatchInnings.SelectMany(i => i.PlayerInnings.Where(pi => pi.DismissedBy != null).Select(pi => pi.DismissedBy));

            var finder = new Mock<IPlayerIdentityFinder>();
            finder.Setup(x => x.PlayerIdentitiesInMatch(_match)).Returns(_playerIdentities);
            var result = new PlayerInMatchStatisticsBuilder(finder.Object).BuildStatisticsForMatch(_match);

            foreach (var identity in fielders)
            {
                foreach (var innings in _match.MatchInnings)
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
            var wicketTakers = _match.MatchInnings.SelectMany(i => i.PlayerInnings.Where(pi => pi.Bowler != null).Select(pi => pi.Bowler));

            var finder = new Mock<IPlayerIdentityFinder>();
            finder.Setup(x => x.PlayerIdentitiesInMatch(_match)).Returns(_playerIdentities);
            var result = new PlayerInMatchStatisticsBuilder(finder.Object).BuildStatisticsForMatch(_match);

            foreach (var identity in wicketTakers)
            {
                foreach (var innings in _match.MatchInnings)
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
            var wicketTakers = _match.MatchInnings.SelectMany(i => i.PlayerInnings.Where(pi => pi.Bowler != null).Select(pi => pi.Bowler));

            var finder = new Mock<IPlayerIdentityFinder>();
            finder.Setup(x => x.PlayerIdentitiesInMatch(_match)).Returns(_playerIdentities);
            var result = new PlayerInMatchStatisticsBuilder(finder.Object).BuildStatisticsForMatch(_match);

            foreach (var identity in wicketTakers)
            {
                foreach (var innings in _match.MatchInnings)
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
            var bowlers = _match.MatchInnings.SelectMany(i => i.BowlingFigures.Select(pi => pi.Bowler));

            var finder = new Mock<IPlayerIdentityFinder>();
            finder.Setup(x => x.PlayerIdentitiesInMatch(_match)).Returns(_playerIdentities);
            var result = new PlayerInMatchStatisticsBuilder(finder.Object).BuildStatisticsForMatch(_match);

            foreach (var identity in bowlers)
            {
                foreach (var innings in _match.MatchInnings)
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
            var bowlers = _match.MatchInnings.SelectMany(i => i.BowlingFigures.Select(pi => pi.Bowler));

            var finder = new Mock<IPlayerIdentityFinder>();
            finder.Setup(x => x.PlayerIdentitiesInMatch(_match)).Returns(_playerIdentities);
            var result = new PlayerInMatchStatisticsBuilder(finder.Object).BuildStatisticsForMatch(_match);

            foreach (var identity in bowlers)
            {
                foreach (var innings in _match.MatchInnings)
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
            var bowlers = _match.MatchInnings.SelectMany(i => i.OversBowled.Select(pi => pi.PlayerIdentity));

            var finder = new Mock<IPlayerIdentityFinder>();
            finder.Setup(x => x.PlayerIdentitiesInMatch(_match)).Returns(_playerIdentities);
            var result = new PlayerInMatchStatisticsBuilder(finder.Object).BuildStatisticsForMatch(_match);

            foreach (var identity in bowlers)
            {
                foreach (var innings in _match.MatchInnings)
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
            var bowlers = _match.MatchInnings.SelectMany(i => i.OversBowled.Select(pi => pi.PlayerIdentity));

            var finder = new Mock<IPlayerIdentityFinder>();
            finder.Setup(x => x.PlayerIdentitiesInMatch(_match)).Returns(_playerIdentities);
            var result = new PlayerInMatchStatisticsBuilder(finder.Object).BuildStatisticsForMatch(_match);

            foreach (var identity in bowlers)
            {
                foreach (var innings in _match.MatchInnings)
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
            var winners = _match.Awards.Select(pi => pi.PlayerIdentity);

            var finder = new Mock<IPlayerIdentityFinder>();
            finder.Setup(x => x.PlayerIdentitiesInMatch(_match)).Returns(_playerIdentities);
            var result = new PlayerInMatchStatisticsBuilder(finder.Object).BuildStatisticsForMatch(_match);

            foreach (var identity in winners)
            {
                foreach (var innings in _match.MatchInnings)
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
            var winners = _match.Awards.Select(pi => pi.PlayerIdentity);

            var finder = new Mock<IPlayerIdentityFinder>();
            finder.Setup(x => x.PlayerIdentitiesInMatch(_match)).Returns(_playerIdentities);
            var result = new PlayerInMatchStatisticsBuilder(finder.Object).BuildStatisticsForMatch(_match);

            foreach (var identity in winners)
            {
                foreach (var innings in _match.MatchInnings)
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
            var playerWhoBattedTwiceInAnInnings = _match.MatchInnings[0].PlayerInnings[0].PlayerIdentity;
            var inningsWhereTheyBattedTwice = _match.MatchInnings[0].MatchInningsId;

            var finder = new Mock<IPlayerIdentityFinder>();
            finder.Setup(x => x.PlayerIdentitiesInMatch(_match)).Returns(_playerIdentities);
            var result = new PlayerInMatchStatisticsBuilder(finder.Object).BuildStatisticsForMatch(_match);

            foreach (var identity in _playerIdentities)
            {
                foreach (var innings in _match.MatchInnings)
                {
                    if (identity.PlayerIdentityId == playerWhoBattedTwiceInAnInnings.PlayerIdentityId && innings.MatchInningsId == inningsWhereTheyBattedTwice)
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
        // playerRecord.BattingPosition
        // playerRecord.DismissalType
        // playerRecord.PlayerWasDismissed
        // playerRecord.BowledByPlayerIdentityId
        // playerRecord.BowledByName
        // playerRecord.BowledByRoute
        // playerRecord.CaughtByPlayerIdentityId
        // playerRecord.CaughtByName
        // playerRecord.CaughtByRoute
        // playerRecord.RunOutByPlayerIdentityId
        // playerRecord.RunOutByName
        // playerRecord.RunOutByRoute
        // playerRecord.RunsScored
        // playerRecord.BallsFaced
        // playerRecord.Catches
        // playerRecord.RunOuts
        // playerRecord.WonMatch
        // playerRecord.PlayerOfTheMatch
    }
}
