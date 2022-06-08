using System;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using Dapper;
using Moq;
using Stoolball.Data.SqlServer.IntegrationTests.Fixtures;
using Stoolball.Matches;
using Stoolball.Routing;
using Xunit;

namespace Stoolball.Data.SqlServer.IntegrationTests.Matches
{
    [Collection(IntegrationTestConstants.DataSourceIntegrationTestCollection)]
    public class SqlServerMatchDataSourceTests : IDisposable
    {
        private readonly SqlServerDataSourceFixture _databaseFixture;
        private readonly TransactionScope _scope;

        public SqlServerMatchDataSourceTests(SqlServerDataSourceFixture databaseFixture)
        {
            _databaseFixture = databaseFixture ?? throw new ArgumentNullException(nameof(databaseFixture));
            _scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
        }

        [Fact]
        public async Task Read_match_by_route_returns_null_for_route_that_does_not_find_a_match()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            routeNormaliser.Setup(x => x.NormaliseRouteToEntity(_databaseFixture.MatchInThePastWithMinimalDetails.MatchRoute, "matches")).Returns("/matches/this-does-not-exist");
            var matchDataSource = new SqlServerMatchDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await matchDataSource.ReadMatchByRoute("/matches/this-does-not-exist").ConfigureAwait(false);

            Assert.Null(result);
        }

        [Fact]
        public async Task Read_match_by_route_reads_minimal_match_in_the_past()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            routeNormaliser.Setup(x => x.NormaliseRouteToEntity(_databaseFixture.MatchInThePastWithMinimalDetails.MatchRoute, "matches")).Returns(_databaseFixture.MatchInThePastWithMinimalDetails.MatchRoute);
            var matchDataSource = new SqlServerMatchDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await matchDataSource.ReadMatchByRoute(_databaseFixture.MatchInThePastWithMinimalDetails.MatchRoute).ConfigureAwait(false);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task Read_match_by_route_reads_minimal_match_in_the_future()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            routeNormaliser.Setup(x => x.NormaliseRouteToEntity(_databaseFixture.MatchInTheFutureWithMinimalDetails.MatchRoute, "matches")).Returns(_databaseFixture.MatchInTheFutureWithMinimalDetails.MatchRoute);
            var matchDataSource = new SqlServerMatchDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await matchDataSource.ReadMatchByRoute(_databaseFixture.MatchInTheFutureWithMinimalDetails.MatchRoute).ConfigureAwait(false);

            Assert.NotNull(result);
        }

        [Fact]
        public async Task Read_match_by_route_returns_basic_match_fields()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            routeNormaliser.Setup(x => x.NormaliseRouteToEntity(_databaseFixture.MatchInThePastWithFullDetails.MatchRoute, "matches")).Returns(_databaseFixture.MatchInThePastWithFullDetails.MatchRoute);
            var matchDataSource = new SqlServerMatchDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await matchDataSource.ReadMatchByRoute(_databaseFixture.MatchInThePastWithFullDetails.MatchRoute).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.MatchInThePastWithFullDetails.MatchId, result.MatchId);
            Assert.Equal(_databaseFixture.MatchInThePastWithFullDetails.MatchName, result.MatchName);
            Assert.Equal(_databaseFixture.MatchInThePastWithFullDetails.MatchType, result.MatchType);
            Assert.Equal(_databaseFixture.MatchInThePastWithFullDetails.PlayerType, result.PlayerType);
            Assert.Equal(_databaseFixture.MatchInThePastWithFullDetails.StartTime, result.StartTime);
            Assert.Equal(_databaseFixture.MatchInThePastWithFullDetails.StartTimeIsKnown, result.StartTimeIsKnown);
            Assert.Equal(_databaseFixture.MatchInThePastWithFullDetails.MatchResultType, result.MatchResultType);
            Assert.Equal(_databaseFixture.MatchInThePastWithFullDetails.PlayersPerTeam, result.PlayersPerTeam);
            Assert.Equal(_databaseFixture.MatchInThePastWithFullDetails.LastPlayerBatsOn, result.LastPlayerBatsOn);
            Assert.Equal(_databaseFixture.MatchInThePastWithFullDetails.EnableBonusOrPenaltyRuns, result.EnableBonusOrPenaltyRuns);
            Assert.Equal(_databaseFixture.MatchInThePastWithFullDetails.InningsOrderIsKnown, result.InningsOrderIsKnown);
            Assert.Equal(_databaseFixture.MatchInThePastWithFullDetails.MatchNotes, result.MatchNotes);
            Assert.Equal(_databaseFixture.MatchInThePastWithFullDetails.MatchRoute, result.MatchRoute);
            Assert.Equal(_databaseFixture.MatchInThePastWithFullDetails.MemberKey, result.MemberKey);
            Assert.Equal(_databaseFixture.MatchInThePastWithFullDetails.UpdateMatchNameAutomatically, result.UpdateMatchNameAutomatically);
        }

        [Fact]
        public async Task Read_match_by_route_returns_teams_with_name_and_route_home_team_first()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            routeNormaliser.Setup(x => x.NormaliseRouteToEntity(_databaseFixture.MatchInThePastWithFullDetails.MatchRoute, "matches")).Returns(_databaseFixture.MatchInThePastWithFullDetails.MatchRoute);
            var matchDataSource = new SqlServerMatchDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await matchDataSource.ReadMatchByRoute(_databaseFixture.MatchInThePastWithFullDetails.MatchRoute).ConfigureAwait(false);

            Assert.Equal(TeamRole.Home, result.Teams[0].TeamRole);
            for (var team = 0; team < _databaseFixture.MatchInThePastWithFullDetails.Teams.Count; team++)
            {
                Assert.Equal(_databaseFixture.MatchInThePastWithFullDetails.Teams[team].Team.TeamName, result.Teams[team].Team.TeamName);
                Assert.Equal(_databaseFixture.MatchInThePastWithFullDetails.Teams[team].Team.TeamRoute, result.Teams[team].Team.TeamRoute);
            }
        }

        [Fact]
        public async Task Read_match_by_route_returns_all_match_innings()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            routeNormaliser.Setup(x => x.NormaliseRouteToEntity(_databaseFixture.MatchInThePastWithFullDetails.MatchRoute, "matches")).Returns(_databaseFixture.MatchInThePastWithFullDetails.MatchRoute);
            var matchDataSource = new SqlServerMatchDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await matchDataSource.ReadMatchByRoute(_databaseFixture.MatchInThePastWithFullDetails.MatchRoute).ConfigureAwait(false);

            for (var innings = 0; innings < _databaseFixture.MatchInThePastWithFullDetails.MatchInnings.Count; innings++)
            {
                Assert.Equal(_databaseFixture.MatchInThePastWithFullDetails.MatchInnings[innings].MatchInningsId, result.MatchInnings[innings].MatchInningsId);
                Assert.Equal(_databaseFixture.MatchInThePastWithFullDetails.MatchInnings[innings].Byes, result.MatchInnings[innings].Byes);
                Assert.Equal(_databaseFixture.MatchInThePastWithFullDetails.MatchInnings[innings].Wides, result.MatchInnings[innings].Wides);
                Assert.Equal(_databaseFixture.MatchInThePastWithFullDetails.MatchInnings[innings].NoBalls, result.MatchInnings[innings].NoBalls);
                Assert.Equal(_databaseFixture.MatchInThePastWithFullDetails.MatchInnings[innings].BonusOrPenaltyRuns, result.MatchInnings[innings].BonusOrPenaltyRuns);
                Assert.Equal(_databaseFixture.MatchInThePastWithFullDetails.MatchInnings[innings].Runs, result.MatchInnings[innings].Runs);
                Assert.Equal(_databaseFixture.MatchInThePastWithFullDetails.MatchInnings[innings].Wickets, result.MatchInnings[innings].Wickets);
                Assert.Equal(_databaseFixture.MatchInThePastWithFullDetails.MatchInnings[innings].InningsOrderInMatch, result.MatchInnings[innings].InningsOrderInMatch);
                Assert.Equal(_databaseFixture.MatchInThePastWithFullDetails.MatchInnings[innings].BattingMatchTeamId, result.MatchInnings[innings].BattingMatchTeamId);
                Assert.Equal(_databaseFixture.MatchInThePastWithFullDetails.MatchInnings[innings].BowlingMatchTeamId, result.MatchInnings[innings].BowlingMatchTeamId);
            }
        }

        [Fact]
        public async Task Read_match_by_route_returns_player_innings()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            routeNormaliser.Setup(x => x.NormaliseRouteToEntity(_databaseFixture.MatchInThePastWithFullDetails.MatchRoute, "matches")).Returns(_databaseFixture.MatchInThePastWithFullDetails.MatchRoute);
            var matchDataSource = new SqlServerMatchDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await matchDataSource.ReadMatchByRoute(_databaseFixture.MatchInThePastWithFullDetails.MatchRoute).ConfigureAwait(false);

            for (var innings = 0; innings < _databaseFixture.MatchInThePastWithFullDetails.MatchInnings.Count; innings++)
            {
                for (var batter = 0; batter < _databaseFixture.MatchInThePastWithFullDetails.MatchInnings[innings].PlayerInnings.Count; batter++)
                {
                    Assert.Equal(_databaseFixture.MatchInThePastWithFullDetails.MatchInnings[innings].PlayerInnings[batter].Batter.PlayerIdentityName, result.MatchInnings[innings].PlayerInnings[batter].Batter.PlayerIdentityName);
                    Assert.Equal(_databaseFixture.MatchInThePastWithFullDetails.MatchInnings[innings].PlayerInnings[batter].Batter.Player.PlayerId, result.MatchInnings[innings].PlayerInnings[batter].Batter.Player.PlayerId);
                    Assert.Equal(_databaseFixture.MatchInThePastWithFullDetails.MatchInnings[innings].PlayerInnings[batter].Batter.Player.PlayerRoute, result.MatchInnings[innings].PlayerInnings[batter].Batter.Player.PlayerRoute);
                    Assert.Equal(_databaseFixture.MatchInThePastWithFullDetails.MatchInnings[innings].PlayerInnings[batter].Batter.Player.PlayerId, result.MatchInnings[innings].PlayerInnings[batter].Batter.Player.PlayerId);
                    Assert.Equal(_databaseFixture.MatchInThePastWithFullDetails.MatchInnings[innings].PlayerInnings[batter].Batter.Team.TeamId, result.MatchInnings[innings].PlayerInnings[batter].Batter.Team.TeamId);
                    Assert.Equal(_databaseFixture.MatchInThePastWithFullDetails.MatchInnings[innings].PlayerInnings[batter].DismissalType, result.MatchInnings[innings].PlayerInnings[batter].DismissalType);
                    Assert.Equal(_databaseFixture.MatchInThePastWithFullDetails.MatchInnings[innings].PlayerInnings[batter].DismissedBy?.PlayerIdentityName, result.MatchInnings[innings].PlayerInnings[batter].DismissedBy?.PlayerIdentityName);
                    Assert.Equal(_databaseFixture.MatchInThePastWithFullDetails.MatchInnings[innings].PlayerInnings[batter].DismissedBy?.Player.PlayerId, result.MatchInnings[innings].PlayerInnings[batter].DismissedBy?.Player.PlayerId);
                    Assert.Equal(_databaseFixture.MatchInThePastWithFullDetails.MatchInnings[innings].PlayerInnings[batter].DismissedBy?.Player.PlayerRoute, result.MatchInnings[innings].PlayerInnings[batter].DismissedBy?.Player.PlayerRoute);
                    Assert.Equal(_databaseFixture.MatchInThePastWithFullDetails.MatchInnings[innings].PlayerInnings[batter].DismissedBy?.Team.TeamId, result.MatchInnings[innings].PlayerInnings[batter].DismissedBy?.Team.TeamId);
                    Assert.Equal(_databaseFixture.MatchInThePastWithFullDetails.MatchInnings[innings].PlayerInnings[batter].Bowler?.PlayerIdentityName, result.MatchInnings[innings].PlayerInnings[batter].Bowler?.PlayerIdentityName);
                    Assert.Equal(_databaseFixture.MatchInThePastWithFullDetails.MatchInnings[innings].PlayerInnings[batter].Bowler?.Player.PlayerId, result.MatchInnings[innings].PlayerInnings[batter].Bowler?.Player.PlayerId);
                    Assert.Equal(_databaseFixture.MatchInThePastWithFullDetails.MatchInnings[innings].PlayerInnings[batter].Bowler?.Player.PlayerRoute, result.MatchInnings[innings].PlayerInnings[batter].Bowler?.Player.PlayerRoute);
                    Assert.Equal(_databaseFixture.MatchInThePastWithFullDetails.MatchInnings[innings].PlayerInnings[batter].Bowler?.Team.TeamId, result.MatchInnings[innings].PlayerInnings[batter].Bowler?.Team.TeamId);
                    Assert.Equal(_databaseFixture.MatchInThePastWithFullDetails.MatchInnings[innings].PlayerInnings[batter].RunsScored, result.MatchInnings[innings].PlayerInnings[batter].RunsScored);
                    Assert.Equal(_databaseFixture.MatchInThePastWithFullDetails.MatchInnings[innings].PlayerInnings[batter].BallsFaced, result.MatchInnings[innings].PlayerInnings[batter].BallsFaced);
                }
            }
        }

        [Fact]
        public async Task Read_match_by_route_does_not_duplicate_player_innings_when_overs_and_oversets_are_duplicated()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            routeNormaliser.Setup(x => x.NormaliseRouteToEntity(_databaseFixture.MatchInThePastWithFullDetails.MatchRoute, "matches")).Returns(_databaseFixture.MatchInThePastWithFullDetails.MatchRoute);
            var matchDataSource = new SqlServerMatchDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);
            var affectedMatchInnings = _databaseFixture.MatchInThePastWithFullDetails.MatchInnings.First();

            using (var connection = _databaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var duplicateOverSetId = Guid.NewGuid();
                await connection.ExecuteAsync(@$"INSERT INTO {Tables.OverSet} (OverSetId, MatchInningsId, OverSetNumber, Overs, BallsPerOver)
                                                 SELECT TOP 1 @duplicateOverSetId, MatchInningsId, OverSetNumber, Overs, BallsPerOver FROM {Tables.OverSet} WHERE MatchInningsId = @MatchInningsId",
                                                 new { duplicateOverSetId, affectedMatchInnings.MatchInningsId });
                await connection.ExecuteAsync(@$"INSERT INTO {Tables.Over} (OverId, MatchInningsId, BowlerPlayerIdentityId, OverNumber, OverSetId, BallsBowled, NoBalls, Wides, RunsConceded) 
                                                 SELECT NEWID(), MatchInningsId, BowlerPlayerIdentityId, OverNumber, @duplicateOverSetId, BallsBowled, NoBalls, Wides, RunsConceded
                                                 FROM {Tables.Over} WHERE MatchInningsId = @MatchInningsId", new { duplicateOverSetId, affectedMatchInnings.MatchInningsId });
            }

            var result = await matchDataSource.ReadMatchByRoute(_databaseFixture.MatchInThePastWithFullDetails.MatchRoute).ConfigureAwait(false);

            var playerInningsIds = result.MatchInnings.Single(x => x.MatchInningsId == affectedMatchInnings.MatchInningsId).PlayerInnings.Select(x => x.PlayerInningsId);
            var distinctPlayerInningsIds = playerInningsIds.Distinct();
            Assert.Equal(distinctPlayerInningsIds.Count(), playerInningsIds.Count());
        }

        [Fact]
        public async Task Read_match_by_route_returns_over_sets()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            routeNormaliser.Setup(x => x.NormaliseRouteToEntity(_databaseFixture.MatchInThePastWithFullDetails.MatchRoute, "matches")).Returns(_databaseFixture.MatchInThePastWithFullDetails.MatchRoute);
            var matchDataSource = new SqlServerMatchDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await matchDataSource.ReadMatchByRoute(_databaseFixture.MatchInThePastWithFullDetails.MatchRoute).ConfigureAwait(false);

            for (var innings = 0; innings < _databaseFixture.MatchInThePastWithFullDetails.MatchInnings.Count; innings++)
            {
                for (var set = 0; set < _databaseFixture.MatchInThePastWithFullDetails.MatchInnings[innings].OverSets.Count; set++)
                {
                    Assert.Equal(_databaseFixture.MatchInThePastWithFullDetails.MatchInnings[innings].OverSets[set].Overs, result.MatchInnings[innings].OverSets[set].Overs);
                    Assert.Equal(_databaseFixture.MatchInThePastWithFullDetails.MatchInnings[innings].OverSets[set].BallsPerOver, result.MatchInnings[innings].OverSets[set].BallsPerOver);
                }
            }
        }

        [Fact]
        public async Task Read_match_by_route_returns_overs_bowled()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            routeNormaliser.Setup(x => x.NormaliseRouteToEntity(_databaseFixture.MatchInThePastWithFullDetails.MatchRoute, "matches")).Returns(_databaseFixture.MatchInThePastWithFullDetails.MatchRoute);
            var matchDataSource = new SqlServerMatchDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await matchDataSource.ReadMatchByRoute(_databaseFixture.MatchInThePastWithFullDetails.MatchRoute).ConfigureAwait(false);

            for (var innings = 0; innings < _databaseFixture.MatchInThePastWithFullDetails.MatchInnings.Count; innings++)
            {
                for (var over = 0; over < _databaseFixture.MatchInThePastWithFullDetails.MatchInnings[innings].OversBowled.Count; over++)
                {
                    Assert.Equal(_databaseFixture.MatchInThePastWithFullDetails.MatchInnings[innings].OversBowled[over].Bowler.PlayerIdentityName, result.MatchInnings[innings].OversBowled[over].Bowler.PlayerIdentityName);
                    Assert.Equal(_databaseFixture.MatchInThePastWithFullDetails.MatchInnings[innings].OversBowled[over].Bowler.Player.PlayerId, result.MatchInnings[innings].OversBowled[over].Bowler.Player.PlayerId);
                    Assert.Equal(_databaseFixture.MatchInThePastWithFullDetails.MatchInnings[innings].OversBowled[over].Bowler.Player.PlayerRoute, result.MatchInnings[innings].OversBowled[over].Bowler.Player.PlayerRoute);
                    Assert.Equal(_databaseFixture.MatchInThePastWithFullDetails.MatchInnings[innings].OversBowled[over].Bowler.Team.TeamId, result.MatchInnings[innings].OversBowled[over].Bowler.Team.TeamId);
                    Assert.Equal(_databaseFixture.MatchInThePastWithFullDetails.MatchInnings[innings].OversBowled[over].BallsBowled, result.MatchInnings[innings].OversBowled[over].BallsBowled);
                    Assert.Equal(_databaseFixture.MatchInThePastWithFullDetails.MatchInnings[innings].OversBowled[over].NoBalls, result.MatchInnings[innings].OversBowled[over].NoBalls);
                    Assert.Equal(_databaseFixture.MatchInThePastWithFullDetails.MatchInnings[innings].OversBowled[over].Wides, result.MatchInnings[innings].OversBowled[over].Wides);
                    Assert.Equal(_databaseFixture.MatchInThePastWithFullDetails.MatchInnings[innings].OversBowled[over].RunsConceded, result.MatchInnings[innings].OversBowled[over].RunsConceded);
                }
            }
        }

        [Fact]
        public async Task Read_match_by_route_returns_bowling_figures()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            routeNormaliser.Setup(x => x.NormaliseRouteToEntity(_databaseFixture.MatchInThePastWithFullDetails.MatchRoute, "matches")).Returns(_databaseFixture.MatchInThePastWithFullDetails.MatchRoute);
            var matchDataSource = new SqlServerMatchDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await matchDataSource.ReadMatchByRoute(_databaseFixture.MatchInThePastWithFullDetails.MatchRoute).ConfigureAwait(false);

            for (var innings = 0; innings < _databaseFixture.MatchInThePastWithFullDetails.MatchInnings.Count; innings++)
            {
                for (var bowler = 0; bowler < _databaseFixture.MatchInThePastWithFullDetails.MatchInnings[innings].BowlingFigures.Count; bowler++)
                {
                    Assert.Equal(_databaseFixture.MatchInThePastWithFullDetails.MatchInnings[innings].BowlingFigures[bowler].BowlingFiguresId, result.MatchInnings[innings].BowlingFigures[bowler].BowlingFiguresId);
                    Assert.Equal(_databaseFixture.MatchInThePastWithFullDetails.MatchInnings[innings].BowlingFigures[bowler].Bowler.PlayerIdentityName, result.MatchInnings[innings].BowlingFigures[bowler].Bowler.PlayerIdentityName);
                    Assert.Equal(_databaseFixture.MatchInThePastWithFullDetails.MatchInnings[innings].BowlingFigures[bowler].Bowler.Player.PlayerId, result.MatchInnings[innings].BowlingFigures[bowler].Bowler.Player.PlayerId);
                    Assert.Equal(_databaseFixture.MatchInThePastWithFullDetails.MatchInnings[innings].BowlingFigures[bowler].Bowler.Player.PlayerRoute, result.MatchInnings[innings].BowlingFigures[bowler].Bowler.Player.PlayerRoute);
                    Assert.Equal(_databaseFixture.MatchInThePastWithFullDetails.MatchInnings[innings].BowlingFigures[bowler].Bowler.Team.TeamId, result.MatchInnings[innings].BowlingFigures[bowler].Bowler.Team.TeamId);
                    Assert.Equal(_databaseFixture.MatchInThePastWithFullDetails.MatchInnings[innings].BowlingFigures[bowler].Overs, result.MatchInnings[innings].BowlingFigures[bowler].Overs);
                    Assert.Equal(_databaseFixture.MatchInThePastWithFullDetails.MatchInnings[innings].BowlingFigures[bowler].Maidens, result.MatchInnings[innings].BowlingFigures[bowler].Maidens);
                    Assert.Equal(_databaseFixture.MatchInThePastWithFullDetails.MatchInnings[innings].BowlingFigures[bowler].RunsConceded, result.MatchInnings[innings].BowlingFigures[bowler].RunsConceded);
                    Assert.Equal(_databaseFixture.MatchInThePastWithFullDetails.MatchInnings[innings].BowlingFigures[bowler].Wickets, result.MatchInnings[innings].BowlingFigures[bowler].Wickets);
                }
            }

        }

        [Fact]
        public async Task Read_match_by_route_returns_awards_with_player_identities_and_reasons()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            routeNormaliser.Setup(x => x.NormaliseRouteToEntity(_databaseFixture.MatchInThePastWithFullDetails.MatchRoute, "matches")).Returns(_databaseFixture.MatchInThePastWithFullDetails.MatchRoute);
            var matchDataSource = new SqlServerMatchDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await matchDataSource.ReadMatchByRoute(_databaseFixture.MatchInThePastWithFullDetails.MatchRoute).ConfigureAwait(false);

            for (var award = 0; award < _databaseFixture.MatchInThePastWithFullDetails.Awards.Count; award++)
            {
                Assert.Equal(_databaseFixture.MatchInThePastWithFullDetails.Awards[award].PlayerIdentity.PlayerIdentityName, result.Awards[award].PlayerIdentity.PlayerIdentityName);
                Assert.Equal(_databaseFixture.MatchInThePastWithFullDetails.Awards[award].PlayerIdentity.Player.PlayerId, result.Awards[award].PlayerIdentity.Player.PlayerId);
                Assert.Equal(_databaseFixture.MatchInThePastWithFullDetails.Awards[award].PlayerIdentity.Player.PlayerRoute, result.Awards[award].PlayerIdentity.Player.PlayerRoute);
                Assert.Equal(_databaseFixture.MatchInThePastWithFullDetails.Awards[award].PlayerIdentity.Team.TeamId, result.Awards[award].PlayerIdentity.Team.TeamId);
                Assert.Equal(_databaseFixture.MatchInThePastWithFullDetails.Awards[award].Reason, result.Awards[award].Reason);
            }
        }

        [Fact]
        public async Task Read_match_by_route_returns_tournament()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            routeNormaliser.Setup(x => x.NormaliseRouteToEntity(_databaseFixture.MatchInThePastWithFullDetailsAndTournament.MatchRoute, "matches")).Returns(_databaseFixture.MatchInThePastWithFullDetailsAndTournament.MatchRoute);
            var matchDataSource = new SqlServerMatchDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await matchDataSource.ReadMatchByRoute(_databaseFixture.MatchInThePastWithFullDetailsAndTournament.MatchRoute).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.MatchInThePastWithFullDetailsAndTournament.Tournament.TournamentId, result.Tournament.TournamentId);
            Assert.Equal(_databaseFixture.MatchInThePastWithFullDetailsAndTournament.Tournament.TournamentName, result.Tournament.TournamentName);
            Assert.Equal(_databaseFixture.MatchInThePastWithFullDetailsAndTournament.Tournament.TournamentRoute, result.Tournament.TournamentRoute);
            Assert.Equal(_databaseFixture.MatchInThePastWithFullDetailsAndTournament.Tournament.MemberKey, result.Tournament.MemberKey);
        }

        [Fact]
        public async Task Read_match_by_route_returns_match_location()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            routeNormaliser.Setup(x => x.NormaliseRouteToEntity(_databaseFixture.MatchInThePastWithFullDetailsAndTournament.MatchRoute, "matches")).Returns(_databaseFixture.MatchInThePastWithFullDetailsAndTournament.MatchRoute);
            var matchDataSource = new SqlServerMatchDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await matchDataSource.ReadMatchByRoute(_databaseFixture.MatchInThePastWithFullDetailsAndTournament.MatchRoute).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.MatchInThePastWithFullDetailsAndTournament.MatchLocation.MatchLocationId, result.MatchLocation.MatchLocationId);
            Assert.Equal(_databaseFixture.MatchInThePastWithFullDetailsAndTournament.MatchLocation.MatchLocationRoute, result.MatchLocation.MatchLocationRoute);
            Assert.Equal(_databaseFixture.MatchInThePastWithFullDetailsAndTournament.MatchLocation.SecondaryAddressableObjectName, result.MatchLocation.SecondaryAddressableObjectName);
            Assert.Equal(_databaseFixture.MatchInThePastWithFullDetailsAndTournament.MatchLocation.PrimaryAddressableObjectName, result.MatchLocation.PrimaryAddressableObjectName);
            Assert.Equal(_databaseFixture.MatchInThePastWithFullDetailsAndTournament.MatchLocation.StreetDescription, result.MatchLocation.StreetDescription);
            Assert.Equal(_databaseFixture.MatchInThePastWithFullDetailsAndTournament.MatchLocation.Locality, result.MatchLocation.Locality);
            Assert.Equal(_databaseFixture.MatchInThePastWithFullDetailsAndTournament.MatchLocation.Town, result.MatchLocation.Town);
            Assert.Equal(_databaseFixture.MatchInThePastWithFullDetailsAndTournament.MatchLocation.AdministrativeArea, result.MatchLocation.AdministrativeArea);
            Assert.Equal(_databaseFixture.MatchInThePastWithFullDetailsAndTournament.MatchLocation.Postcode, result.MatchLocation.Postcode);
            Assert.Equal(_databaseFixture.MatchInThePastWithFullDetailsAndTournament.MatchLocation.Latitude, result.MatchLocation.Latitude);
            Assert.Equal(_databaseFixture.MatchInThePastWithFullDetailsAndTournament.MatchLocation.Longitude, result.MatchLocation.Longitude);
            Assert.Equal(_databaseFixture.MatchInThePastWithFullDetailsAndTournament.MatchLocation.GeoPrecision, result.MatchLocation.GeoPrecision);
            Assert.Equal(_databaseFixture.MatchInThePastWithFullDetailsAndTournament.MatchLocation.MatchLocationNotes, result.MatchLocation.MatchLocationNotes);
        }

        [Fact]
        public async Task Read_match_by_route_returns_season()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            routeNormaliser.Setup(x => x.NormaliseRouteToEntity(_databaseFixture.MatchInThePastWithFullDetailsAndTournament.MatchRoute, "matches")).Returns(_databaseFixture.MatchInThePastWithFullDetailsAndTournament.MatchRoute);
            var matchDataSource = new SqlServerMatchDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await matchDataSource.ReadMatchByRoute(_databaseFixture.MatchInThePastWithFullDetailsAndTournament.MatchRoute).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.MatchInThePastWithFullDetailsAndTournament.Season.SeasonId, result.Season.SeasonId);
            Assert.Equal(_databaseFixture.MatchInThePastWithFullDetailsAndTournament.Season.SeasonRoute, result.Season.SeasonRoute);
            Assert.Equal(_databaseFixture.MatchInThePastWithFullDetailsAndTournament.Season.FromYear, result.Season.FromYear);
            Assert.Equal(_databaseFixture.MatchInThePastWithFullDetailsAndTournament.Season.UntilYear, result.Season.UntilYear);
        }

        [Fact]
        public async Task Read_match_by_route_returns_competition()
        {
            var routeNormaliser = new Mock<IRouteNormaliser>();
            routeNormaliser.Setup(x => x.NormaliseRouteToEntity(_databaseFixture.MatchInThePastWithFullDetailsAndTournament.MatchRoute, "matches")).Returns(_databaseFixture.MatchInThePastWithFullDetailsAndTournament.MatchRoute);
            var matchDataSource = new SqlServerMatchDataSource(_databaseFixture.ConnectionFactory, routeNormaliser.Object);

            var result = await matchDataSource.ReadMatchByRoute(_databaseFixture.MatchInThePastWithFullDetailsAndTournament.MatchRoute).ConfigureAwait(false);

            Assert.Equal(_databaseFixture.MatchInThePastWithFullDetailsAndTournament.Season.Competition.CompetitionId, result.Season.Competition.CompetitionId);
            Assert.Equal(_databaseFixture.MatchInThePastWithFullDetailsAndTournament.Season.Competition.CompetitionName, result.Season.Competition.CompetitionName);
            Assert.Equal(_databaseFixture.MatchInThePastWithFullDetailsAndTournament.Season.Competition.MemberGroupName, result.Season.Competition.MemberGroupName);
            Assert.Equal(_databaseFixture.MatchInThePastWithFullDetailsAndTournament.Season.Competition.CompetitionRoute, result.Season.Competition.CompetitionRoute);
        }
        public void Dispose() => _scope.Dispose();
    }
}
