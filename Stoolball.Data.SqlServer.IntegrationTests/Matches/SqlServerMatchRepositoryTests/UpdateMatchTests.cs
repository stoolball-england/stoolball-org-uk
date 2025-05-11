using System;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dapper;
using Moq;
using Stoolball.Data.SqlServer.IntegrationTests.Fixtures;
using Stoolball.Logging;
using Stoolball.Matches;
using Stoolball.Teams;
using Stoolball.Testing;
using Xunit;
using static Stoolball.Constants;

namespace Stoolball.Data.SqlServer.IntegrationTests.Matches.SqlServerMatchRepositoryTests
{
    [Collection(IntegrationTestConstants.TestDataIntegrationTestCollection)]
    public class UpdateMatchTests : MatchRepositoryTestsBase, IDisposable
    {
        public UpdateMatchTests(SqlServerTestDataFixture databaseFixture) : base(databaseFixture) { }

        [Fact]
        public async Task Throws_ArgumentNullException_if_match_is_null()
        {
            var repo = CreateRepository();

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await repo.UpdateMatch(null!, MemberKey, MemberName));
        }

        [Theory]
        [InlineData(null, true)]
        [InlineData("", true)]
        [InlineData("   ", true)]
        [InlineData("/matches/valid-route", false)]
        public async Task Throws_ArgumentException_if_MatchRoute_is_null_or_whitespace(string? route, bool expectException)
        {
            var match = CloneValidMatch(DatabaseFixture.TestData.Matches.First(m => m.StartTime.UtcDateTime > DateTime.UtcNow));
            match.MatchRoute = route;

            var repo = CreateRepository();

            if (expectException)
            {
                await Assert.ThrowsAsync<ArgumentException>(async () => await repo.UpdateMatch(match, MemberKey, MemberName));
            }
            else
            {
                var exception = await Record.ExceptionAsync(async () => await repo.UpdateMatch(match, MemberKey, MemberName));
                Assert.Null(exception);
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task Throws_ArgumentNullException_if_memberName_is_null_or_whitespace(string? memberName)
        {
            var match = CloneValidMatch(DatabaseFixture.TestData.Matches.First(m => m.StartTime.UtcDateTime > DateTime.UtcNow));

            var repo = CreateRepository();

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await repo.UpdateMatch(match, MemberKey, memberName!));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Throws_ArgumentException_if_a_team_does_not_have_a_TeamId(bool teamIsNull)
        {
            var repo = CreateRepository();
            var match = CloneValidMatch(DatabaseFixture.TestData.Matches.First(m => m.Teams.Any(t => t.Team is not null)));
            match.StartTime = DateTimeOffset.UtcNow.AddDays(1);
            match.Teams[0].Team = teamIsNull ? null : new Team();

            await Assert.ThrowsAsync<ArgumentException>(async () => await repo.UpdateMatch(match, MemberKey, MemberName));
        }

        [Fact]
        public async Task Throws_ArgumentException_if_season_is_not_null_but_does_not_have_a_SeasonId()
        {
            var repo = CreateRepository();
            var match = CloneValidMatch(DatabaseFixture.TestData.Matches.First(m => m.Season is not null));
            match.StartTime = DateTimeOffset.UtcNow.AddDays(1);
            match.Season!.SeasonId = null;

            await Assert.ThrowsAsync<ArgumentException>(async () => await repo.UpdateMatch(match, MemberKey, MemberName));
        }

        [Fact]
        public async Task Throws_ArgumentException_if_match_date_before_SQL_Server_minimum()
        {
            var repo = CreateRepository();
            var match = CloneValidMatch(DatabaseFixture.TestData.Matches.First());
            match.StartTime = new DateTimeOffset(1750, 1, 1, 0, 0, 0, TimeSpan.Zero);

            await Assert.ThrowsAsync<ArgumentException>(async () => await repo.UpdateMatch(match, MemberKey, MemberName));
        }

        [Fact]
        public async Task Throws_ArgumentException_if_match_date_in_the_past()
        {
            var repo = CreateRepository();
            var match = CloneValidMatch(DatabaseFixture.TestData.MatchInThePastWithFullDetails!);

            await Assert.ThrowsAsync<ArgumentException>(async () => await repo.UpdateMatch(match, MemberKey, MemberName));
        }

        [Fact]
        public async Task Throws_ArgumentException_if_team_added_twice_to_training_session()
        {
            var repo = CreateRepository();

            var match = CloneValidMatch(DatabaseFixture.TestData.Matches.First(m => m.MatchType == MatchType.TrainingSession && m.StartTime.UtcDateTime > DateTime.UtcNow));
            var anyExistingTeam = match.Teams.First(t => t.Team is not null).Team;
            match.Teams.Add(new TeamInMatch { Team = anyExistingTeam, TeamRole = TeamRole.Training });

            await Assert.ThrowsAsync<ArgumentException>(async () => await repo.UpdateMatch(match, MemberKey, MemberName));
        }

        //// TODO: If TeamName is missing, in PopulateTeamNames it doesn't populate PlayingAsTeamName - gets latest teamname as opposed to the one at the time of the match

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Route_is_based_on_match_name_if_present(bool matchHasTeams)
        {
            var expectedRoute = $"/matches/expected-route-{Guid.NewGuid()}";
            var match = CloneValidMatch(DatabaseFixture.TestData.Matches.First(m => m.Teams.Any() == matchHasTeams));
            match.StartTime = DateTime.UtcNow.AddDays(1);
            match.MatchName = "Match " + Guid.NewGuid().ToString();

            RouteGenerator.Setup(x => x.GenerateRoute("/matches", $"{match.MatchName} {match.StartTime.ToString("dMMMyyyy", CultureInfo.CurrentCulture)}", NoiseWords.MatchRoute)).Returns(expectedRoute);

            var repo = CreateRepository();

            var updated = await repo.UpdateMatch(match, MemberKey, MemberName);

            Assert.Equal(expectedRoute, updated.MatchRoute);

            await AssertMatchRouteSaved(updated.MatchId, updated.MatchRoute).ConfigureAwait(false);
        }

        [Fact]
        public async Task Route_based_on_home_team_name_then_away_team_name_if_no_match_name_and_teams_added()
        {
            var expectedRoute = $"/matches/expected-route-{Guid.NewGuid()}";
            var matchToClone = DatabaseFixture.TestData.Matches.First(
                                    m => m.Teams.Any(t => t.Team is not null && t.TeamRole == TeamRole.Home)
                                    && m.Teams.Any(t => t.Team is not null && t.TeamRole == TeamRole.Away));
            var match = CloneValidMatch(matchToClone);
            match.StartTime = DateTimeOffset.UtcNow.AddDays(1);
            match.MatchName = null;
            var awayTeam = matchToClone.Teams.First(t => t.Team is not null && t.TeamRole == TeamRole.Away).Team!;
            var homeTeam = matchToClone.Teams.First(t => t.Team is not null && t.TeamRole == TeamRole.Home).Team!;
            match.StartTime = DateTime.UtcNow.AddDays(1);

            MatchNameBuilder.Setup(x => x.BuildMatchName(It.IsAny<Stoolball.Matches.Match>())).Returns("Match " + Guid.NewGuid().ToString());
            RouteGenerator.Setup(x => x.GenerateRoute("/matches", $"{homeTeam.TeamName} {awayTeam.TeamName} {match.StartTime.ToString("dMMMyyyy", CultureInfo.CurrentCulture)}", NoiseWords.MatchRoute)).Returns(expectedRoute);

            var repo = CreateRepository();

            var updated = await repo.UpdateMatch(match, MemberKey, MemberName);

            Assert.Equal(expectedRoute, updated.MatchRoute);

            await AssertMatchRouteSaved(updated.MatchId, updated.MatchRoute).ConfigureAwait(false);
        }

        [Fact]
        public async Task Route_to_be_confirmed_if_no_match_name_and_no_teams()
        {
            var expectedRoute = $"/matches/to-be-confirmed-{Guid.NewGuid()}";
            var match = CloneValidMatch(DatabaseFixture.TestData.Matches.First(m => m.StartTime.UtcDateTime > DateTime.UtcNow));
            match.StartTime = DateTime.UtcNow.AddDays(1);
            match.MatchName = null;
            match.Teams.Clear();
            match.MatchRoute = "provided-route-should-be-ignored";

            MatchNameBuilder.Setup(x => x.BuildMatchName(It.IsAny<Stoolball.Matches.Match>())).Returns("Match " + Guid.NewGuid().ToString());
            RouteGenerator.Setup(x => x.GenerateRoute("/matches", $"to-be-confirmed {match.StartTime.ToString("dMMMyyyy", CultureInfo.CurrentCulture)}", NoiseWords.MatchRoute)).Returns(expectedRoute);

            var repo = CreateRepository();

            var updated = await repo.UpdateMatch(match, MemberKey, MemberName);

            Assert.Equal(expectedRoute, updated.MatchRoute);

            await AssertMatchRouteSaved(updated.MatchId, updated.MatchRoute).ConfigureAwait(false);
        }

        [Fact]
        public async Task Route_gets_numeric_suffix_if_route_has_changed_and_new_route_in_use()
        {
            var routeInUse = DatabaseFixture.TestData.Matches.First(m => m.MatchRoute is not null && Regex.IsMatch(m.MatchRoute, "[a-z]$")).MatchRoute!;
            var expectedRoute = $"/matches/expected-route-{Guid.NewGuid()}-1";
            var match = CloneValidMatch(DatabaseFixture.TestData.Matches.First(
                            m => m.MatchRoute is not null
                            && Regex.IsMatch(m.MatchRoute, "[a-z]$")
                            && m.MatchRoute != routeInUse));
            match.StartTime = DateTime.UtcNow.AddDays(1);

            RouteGenerator.Setup(x => x.GenerateRoute("/matches", $"{match.MatchName} {match.StartTime.ToString("dMMMyyyy", CultureInfo.CurrentCulture)}", NoiseWords.MatchRoute)).Returns(routeInUse);
            RouteGenerator.Setup(x => x.IsMatchingRoute(match.MatchRoute!, routeInUse)).Returns(false);
            RouteGenerator.Setup(x => x.IncrementRoute(routeInUse)).Returns(expectedRoute);

            var repo = CreateRepository();

            var updated = await repo.UpdateMatch(match, MemberKey, MemberName);

            Assert.Equal(expectedRoute, updated.MatchRoute);

            await AssertMatchRouteSaved(updated.MatchId, updated.MatchRoute).ConfigureAwait(false);
        }

        [Fact]
        public async Task Route_when_unchanged_is_not_incremented()
        {
            var match = CloneValidMatch(DatabaseFixture.TestData.Matches.First(m => m.MatchRoute is not null));
            match.StartTime = DateTime.UtcNow.AddDays(1);

            RouteGenerator.Setup(x => x.GenerateRoute("/matches", $"{match.MatchName} {match.StartTime.ToString("dMMMyyyy", CultureInfo.CurrentCulture)}", NoiseWords.MatchRoute)).Returns(match.MatchRoute!);
            RouteGenerator.Setup(x => x.IsMatchingRoute(match.MatchRoute!, match.MatchRoute!)).Returns(true);
            RouteGenerator.Verify(x => x.IncrementRoute(It.IsAny<string>()), Times.Never);

            var repo = CreateRepository();

            var updated = await repo.UpdateMatch(match, MemberKey, MemberName);

            Assert.Equal(match.MatchRoute, updated.MatchRoute);

            await AssertMatchRouteSaved(updated.MatchId, updated.MatchRoute).ConfigureAwait(false);
        }

        [Fact]
        public async Task Inserts_redirect_if_route_changes()
        {
            var match = CloneValidMatch(DatabaseFixture.TestData.MatchInTheFutureWithMinimalDetails!);

            var repo = CreateRepository();

            var updated = await repo.UpdateMatch(match, MemberKey, MemberName);

            RedirectsRepository.Verify(x => x.InsertRedirect(match.MatchRoute!, updated.MatchRoute!, null, It.IsAny<IDbTransaction>()));
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(false, false)]
        public async Task Sets_basic_fields(bool startTimeIsKnown, bool hasSeason)
        {
            var match = CloneValidMatch(DatabaseFixture.TestData.MatchInTheFutureWithMinimalDetails!);

            match.StartTime = DateTimeOffset.Now.AddDays(1);
            match.StartTimeIsKnown = startTimeIsKnown;
            match.MatchName = "Match " + Guid.NewGuid().ToString();
            match.MatchLocation = DatabaseFixture.TestData.MatchLocations.First();
            match.MatchResultType = DatabaseFixture.Randomiser.RandomEnum<MatchResultType>();
            match.MatchNotes = Guid.NewGuid().ToString();
            match.Season = hasSeason ? DatabaseFixture.TestData.SeasonWithMinimalDetails : null;

            if (hasSeason)
            {
                SeasonDataSource.Setup(x => x.ReadSeasonById(match.Season!.SeasonId!.Value, true)).ReturnsAsync(match.Season);
            }

            var repo = CreateRepository();

            var updated = await repo.UpdateMatch(match, MemberKey, MemberName);

            Assert.Equal(match.StartTime, updated.StartTime);
            Assert.Equal(match.StartTimeIsKnown, updated.StartTimeIsKnown);
            Assert.Equal(match.MatchName, updated.MatchName);
            Assert.False(updated.UpdateMatchNameAutomatically);
            Assert.Equal(match.MatchLocation.MatchLocationId, updated.MatchLocation?.MatchLocationId);
            Assert.Equal(match.MatchResultType, updated.MatchResultType);
            Assert.Equal(match.MatchNotes, updated.MatchNotes);
            Assert.Equal(match.Season?.SeasonId, updated.Season?.SeasonId);

            using (var connection = DatabaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var saved = await connection.QuerySingleOrDefaultAsync<(
                    DateTimeOffset StartTime,
                    bool StartTimeIsKnown,
                    string MatchName,
                    Guid? MatchLocationId,
                    string MatchResultType,
                    string MatchNotes,
                    bool UpdateMatchNameAutomatically,
                    Guid? SeasonId)>(
                    @$"SELECT StartTime, StartTimeIsKnown, MatchName, MatchLocationId, MatchResultType, MatchNotes, UpdateMatchNameAutomatically, SeasonId
                       FROM {Tables.Match} WHERE MatchId = @MatchId", new { updated.MatchId }).ConfigureAwait(false);

                Assert.Equal(match.StartTime.AccurateToTheMinute(), saved.StartTime.AccurateToTheMinute());
                Assert.Equal(match.StartTimeIsKnown, saved.StartTimeIsKnown);
                Assert.Equal(match.MatchName, saved.MatchName);
                Assert.Equal(updated.UpdateMatchNameAutomatically, saved.UpdateMatchNameAutomatically);
                Assert.Equal(match.MatchLocation.MatchLocationId, saved.MatchLocationId);
                Assert.Equal(match.MatchResultType.ToString(), saved.MatchResultType);
                Assert.Equal(match.MatchNotes, saved.MatchNotes);
                Assert.Equal(match.Season?.SeasonId, saved.SeasonId);
            }
        }

        [Fact]
        public async Task Sets_MatchName_from_IMatchNameBuilder_when_name_not_present()
        {
            var match = CloneValidMatch(DatabaseFixture.TestData.Matches.First(m => m.StartTime.UtcDateTime > DateTime.UtcNow));
            match.MatchName = null;
            var expectedMatchName = "Match " + Guid.NewGuid().ToString();
            MatchNameBuilder.Setup(x => x.BuildMatchName(It.IsAny<Stoolball.Matches.Match>())).Returns(expectedMatchName);

            var repo = CreateRepository();

            var updated = await repo.UpdateMatch(match, MemberKey, MemberName);

            Assert.Equal(expectedMatchName, updated.MatchName);

            using (var connection = DatabaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var saved = await connection.QuerySingleOrDefaultAsync<(string? MatchName, bool UpdateMatchNameAutomatically)>(
                    $"SELECT MatchName, UpdateMatchNameAutomatically FROM {Tables.Match} WHERE MatchId = @MatchId", new { updated.MatchId }).ConfigureAwait(false);

                Assert.Equal(expectedMatchName, saved.MatchName);
                Assert.True(saved.UpdateMatchNameAutomatically);
            }
        }

        //// TODO: Adds_team_to_training_session will need to test PlayingAsTeamName

        [Fact]
        public async Task Adds_team_to_training_session()
        {
            var match = CloneValidMatch(DatabaseFixture.TestData.Matches.First(m => m.MatchType == MatchType.TrainingSession && m.StartTime.UtcDateTime > DateTime.UtcNow));
            var anyOtherTeam = DatabaseFixture.TestData.Teams.First(t => !match.Teams.Where(x => x.Team is not null).Select(x => x.Team!.TeamId).Contains(t.TeamId));
            match.Teams.Add(new TeamInMatch { Team = anyOtherTeam, TeamRole = TeamRole.Training });
            var expectedNumberOfTeams = match.Teams.Count;

            var repo = CreateRepository();

            var updated = await repo.UpdateMatch(match, MemberKey, MemberName);

            await AssertTeamsInMatch(match, expectedNumberOfTeams, updated).ConfigureAwait(false);
        }

        [Fact]
        public async Task Removes_team_from_training_session()
        {
            var match = CloneValidMatch(DatabaseFixture.TestData.Matches.First(m =>
                m.MatchType == MatchType.TrainingSession &&
                m.StartTime.UtcDateTime > DateTime.UtcNow &&
                m.Teams.Any()));
            var teamToRemove = match.Teams.First(t => t.Team is not null && t.TeamRole == TeamRole.Training);
            match.Teams.Remove(teamToRemove);
            var expectedNumberOfTeams = match.Teams.Count;

            var repo = CreateRepository();

            var updated = await repo.UpdateMatch(match, MemberKey, MemberName);

            Assert.Equal(expectedNumberOfTeams, updated.Teams.Count);
            foreach (var team in match.Teams)
            {
                Assert.Contains(updated.Teams, t =>
                    t.MatchTeamId.HasValue
                    && t.Team?.TeamId == team.Team!.TeamId
                    && t.TeamRole == team.TeamRole
                    && t.BattedFirst is null
                    && t.WonToss is null);
            }

            using (var connection = DatabaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var saved = (await connection.QueryAsync<(Guid TeamId, string TeamRole, bool? WonToss, Guid? WinnerOfMatchId)>(
                    $"SELECT TeamId, TeamRole, WonToss, WinnerOfMatchId FROM {Tables.MatchTeam} WHERE MatchId = @MatchId", new { updated.MatchId }).ConfigureAwait(false)).ToList();
                Assert.Equal(expectedNumberOfTeams, saved.Count);
                foreach (var team in match.Teams)
                {
                    Assert.Contains(saved, t =>
                        t.TeamId == team.Team!.TeamId
                        && t.TeamRole == team.TeamRole.ToString()
                        && t.WonToss is null
                        && t.WinnerOfMatchId is null);
                }
            }
        }

        //// TODO: Adds_team_to_match_in_vacant_role will need to test PlayingAsTeamName
        [Theory]
        [InlineData(TeamRole.Home, false)]
        [InlineData(TeamRole.Away, false)]
        [InlineData(TeamRole.Home, true)]
        [InlineData(TeamRole.Away, true)]
        public async Task Adds_team_to_match_in_vacant_role(TeamRole teamRole, bool matchHasMultipleInnings)
        {
            var match = SetupAddRemoveTeam(teamRole, matchHasMultipleInnings, false, false, true);
            var expectedNumberOfTeams = match.Teams.Count;

            var repo = CreateRepository();

            var updated = await repo.UpdateMatch(match, MemberKey, MemberName);

            await AssertTeamsInMatch(match, expectedNumberOfTeams, updated).ConfigureAwait(false);

            await AssertMatchInnings(updated).ConfigureAwait(false);
        }

        private async Task AssertMatchInnings(Stoolball.Matches.Match updated)
        {
            using (var connection = DatabaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var saved = (await connection.QueryAsync<(int InningsOrderInMatch, Guid? BattingMatchTeamId, Guid? BowlingMatchTeamId)>(
                    $"SELECT InningsOrderInMatch, BattingMatchTeamId, BowlingMatchTeamId FROM {Tables.MatchInnings} WHERE MatchId = @MatchId", new { updated.MatchId }).ConfigureAwait(false)).ToList();

                var homeMatchTeamId = updated.Teams.SingleOrDefault(t => t.TeamRole == TeamRole.Home)?.MatchTeamId;
                var awayMatchTeamId = updated.Teams.SingleOrDefault(t => t.TeamRole == TeamRole.Away)?.MatchTeamId;

                foreach (var innings in saved)
                {
                    if (innings.InningsOrderInMatch % 2 == 1)
                    {
                        // Odd innings order means home team bats first
                        Assert.Equal(homeMatchTeamId, innings.BattingMatchTeamId);
                        Assert.Equal(awayMatchTeamId, innings.BowlingMatchTeamId);
                    }
                    else
                    {
                        // Even innings order means away team bats first
                        Assert.Equal(awayMatchTeamId, innings.BattingMatchTeamId);
                        Assert.Equal(homeMatchTeamId, innings.BowlingMatchTeamId);
                    }
                }
            }
        }

        private async Task AssertTeamsInMatch(Stoolball.Matches.Match match, int expectedNumberOfTeams, Stoolball.Matches.Match updated)
        {
            Assert.Equal(expectedNumberOfTeams, updated.Teams.Count);
            foreach (var team in match.Teams)
            {
                Assert.Contains(updated.Teams, t =>
                    t.MatchTeamId.HasValue
                    && t.Team?.TeamId == team.Team!.TeamId
                    && t.TeamRole == team.TeamRole
                    && t.BattedFirst is null
                    && t.WonToss is null);
            }

            using (var connection = DatabaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var saved = (await connection.QueryAsync<(Guid TeamId, string TeamRole, bool? WonToss, Guid? WinnerOfMatchId)>(
                    $"SELECT TeamId, TeamRole, WonToss, WinnerOfMatchId FROM {Tables.MatchTeam} WHERE MatchId = @MatchId", new { updated.MatchId }).ConfigureAwait(false)).ToList();
                Assert.Equal(expectedNumberOfTeams, saved.Count);
                foreach (var team in match.Teams)
                {
                    Assert.Contains(saved, t =>
                        t.TeamId == team.Team!.TeamId
                        && t.TeamRole == team.TeamRole.ToString()
                        && t.WonToss is null
                        && t.WinnerOfMatchId is null);
                }
            }
        }

        //// TODO: Replaces_team_in_role will need to test PlayingAsTeamName
        [Theory]
        [InlineData(TeamRole.Home, false)]
        [InlineData(TeamRole.Away, false)]
        [InlineData(TeamRole.Home, true)]
        [InlineData(TeamRole.Away, true)]
        public async Task Replaces_team_in_role(TeamRole teamRole, bool matchHasMultipleInnings)
        {
            var match = SetupAddRemoveTeam(teamRole, matchHasMultipleInnings, true, true, true);

            var expectedNumberOfTeams = match.Teams.Count;

            var repo = CreateRepository();

            var updated = await repo.UpdateMatch(match, MemberKey, MemberName);

            await AssertTeamsInMatch(match, expectedNumberOfTeams, updated).ConfigureAwait(false);

            await AssertMatchInnings(updated).ConfigureAwait(false);
        }

        private Stoolball.Matches.Match SetupAddRemoveTeam(TeamRole teamRole, bool matchHasMultipleInnings, bool matchHasTeams, bool removeTeam, bool addTeam)
        {
            var match = CloneValidMatch(DatabaseFixture.TestData.Matches.First(m =>
                            m.MatchType != MatchType.TrainingSession &&
                            m.StartTime.UtcDateTime > DateTime.UtcNow &&
                            m.Teams.Any(t => t.TeamRole == teamRole) == matchHasTeams &&
                            (matchHasMultipleInnings ? m.MatchInnings.Count > 2 : m.MatchInnings.Count == 2)
                            ));

            var initialTeamsInMatch = match.Teams.Where(t => t.Team?.TeamId is not null).Select(t => t.Team!.TeamId!.Value).ToList();
            if (removeTeam)
            {
                match.Teams.Remove(match.Teams.Single(t => t.TeamRole == teamRole));
            }

            if (addTeam)
            {
                var anyOtherTeam = DatabaseFixture.TestData.Teams.First(t => t.TeamId is not null && !initialTeamsInMatch.Contains(t.TeamId.Value));
                match.Teams.Add(new TeamInMatch { Team = anyOtherTeam, TeamRole = teamRole });
            }

            return match;
        }

        [Theory]
        [InlineData(TeamRole.Home, false)]
        [InlineData(TeamRole.Away, false)]
        [InlineData(TeamRole.Home, true)]
        [InlineData(TeamRole.Away, true)]
        public async Task Removes_team_in_role(TeamRole teamRole, bool matchHasMultipleInnings)
        {
            var match = SetupAddRemoveTeam(teamRole, matchHasMultipleInnings, true, true, false);

            var expectedNumberOfTeams = match.Teams.Count;

            var repo = CreateRepository();

            var updated = await repo.UpdateMatch(match, MemberKey, MemberName);

            await AssertTeamsInMatch(match, expectedNumberOfTeams, updated).ConfigureAwait(false);

            await AssertMatchInnings(updated).ConfigureAwait(false);
        }


        [Fact]
        public async Task Audits_and_logs()
        {
            var match = CloneValidMatch(DatabaseFixture.TestData.Matches.First(m => m.StartTime.UtcDateTime > DateTime.UtcNow));

            var repo = CreateRepository();

            var updated = await repo.UpdateMatch(match, MemberKey, MemberName);

            AuditRepository.Verify(x => x.CreateAudit(It.IsAny<AuditRecord>(), It.IsAny<IDbTransaction>()), Times.Once);
            Logger.Verify(x => x.Info(LoggingTemplates.Updated,
                                       It.Is<Stoolball.Matches.Match>(x => x.StartTime.AccurateToTheMinute() == match.StartTime.AccurateToTheMinute()
                                                                        && x.MatchName == match.MatchName),
                                       MemberName, MemberKey, typeof(SqlServerMatchRepository), nameof(SqlServerMatchRepository.UpdateMatch)));
        }
    }
}
