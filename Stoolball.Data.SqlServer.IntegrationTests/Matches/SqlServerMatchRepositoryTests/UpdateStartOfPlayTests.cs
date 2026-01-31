using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Moq;
using Stoolball.Data.SqlServer.IntegrationTests.Fixtures;
using Stoolball.Matches;
using Stoolball.Statistics;
using Xunit;
using static Stoolball.Constants;

namespace Stoolball.Data.SqlServer.IntegrationTests.Matches.SqlServerMatchRepositoryTests
{
    [Collection(IntegrationTestConstants.TestDataIntegrationTestCollection)]
    public class UpdateStartOfPlayTests : MatchRepositoryTestsBase, IDisposable
    {
        private const string UPDATED_MATCH_NAME = "Updated match name";

        public UpdateStartOfPlayTests(SqlServerTestDataFixture databaseFixture) : base(databaseFixture)
        {
            MatchNameBuilder.Setup(x => x.BuildMatchName(It.IsAny<Stoolball.Matches.Match>())).Returns(UPDATED_MATCH_NAME);
        }

        [Fact]
        public async Task UpdateStartOfPlay_throws_MatchNotFoundException_for_match_id_that_does_not_exist()
        {
            var match = CloneValidMatch(DatabaseFixture.TestData.Matches.First(x => x.StartTime < DateTimeOffset.UtcNow &&
                                                                                    x.Teams.Count == 2 &&
                                                                                    x.MatchResultType == null));
            match.MatchId = Guid.NewGuid();

            await Assert.ThrowsAsync<MatchNotFoundException>(
                async () => await Repository.UpdateStartOfPlay(
                    match,
                    MemberKey,
                    MemberName
                ).ConfigureAwait(false)).ConfigureAwait(false);
        }

        [Fact]
        public async Task Route_based_on_match_name_if_match_name_not_updated_automatically()
        {
            await TestThatUpdateStartOfPlayUpdatesRoute(match => match.MatchName!, false).ConfigureAwait(false);
        }

        [Fact]
        public async Task Route_based_on_home_team_name_then_away_team_name_if_match_name_updated_automatically()
        {
            await TestThatUpdateStartOfPlayUpdatesRoute(match =>
            {
                var awayTeam = match.Teams.First(t => t.Team is not null && t.TeamRole == TeamRole.Away).Team!;
                var homeTeam = match.Teams.First(t => t.Team is not null && t.TeamRole == TeamRole.Home).Team!;
                return $"{homeTeam.TeamName} {awayTeam.TeamName}";
            }, true).ConfigureAwait(false);
        }

        private async Task TestThatUpdateStartOfPlayUpdatesRoute(Func<Stoolball.Matches.Match, string> routePrefix, bool matchNameUpdatedAutomatically)
        {
            var expectedRoute = $"/matches/expected-route-{Guid.NewGuid()}";
            var matchToClone = DatabaseFixture.TestData.Matches.First(
                                    m => m.Teams.Any(t => t.Team is not null && t.TeamRole == TeamRole.Home)
                                    && m.Teams.Any(t => t.Team is not null && t.TeamRole == TeamRole.Away)
                                    && m.StartTime > DateTimeOffset.UtcNow
                                    && m.MatchType == MatchType.KnockoutMatch);
            var match = CloneValidMatch(matchToClone);
            match.UpdateMatchNameAutomatically = matchNameUpdatedAutomatically;

            MatchNameBuilder.Setup(x => x.BuildMatchName(It.IsAny<Stoolball.Matches.Match>())).Returns("Match " + Guid.NewGuid().ToString());
            RouteGenerator.Setup(x => x.GenerateRoute("/matches", $"{routePrefix(match)} {match.StartTime.ToString("dMMMyyyy", CultureInfo.CurrentCulture)}", NoiseWords.MatchRoute)).Returns(expectedRoute);

            var updated = await Repository.UpdateStartOfPlay(match, MemberKey, MemberName);

            Assert.Equal(expectedRoute, updated.MatchRoute);

            await AssertMatchRouteSaved(updated.MatchId, updated.MatchRoute).ConfigureAwait(false);
        }

        [Fact]
        public async Task Route_gets_numeric_suffix_if_route_has_changed_and_new_route_in_use()
        {
            await TestThatRouteGetsNumericSuffixIfRouteHasChangedAndNewRouteInUse(match => Repository.UpdateStartOfPlay(match, MemberKey, MemberName),
                                                                                  match => match.StartTime < DateTimeOffset.UtcNow
                                                                                           && match.MatchResultType == null
                                                                                           && match.MatchInnings.Any(mi => mi.BattingTeam != null)).ConfigureAwait(false);
        }

        [Fact]
        public async Task Route_when_unchanged_is_not_incremented()
        {
            await TestThatRouteWhenUnchangedIsNotIncremented(match => Repository.UpdateStartOfPlay(match, MemberKey, MemberName),
                                                             match => match.StartTime < DateTimeOffset.UtcNow
                                                                      && match.MatchResultType == null
                                                                      && match.MatchInnings.Any(mi => mi.BattingTeam != null)).ConfigureAwait(false);
        }

        [Fact]
        public async Task Inserts_redirect_if_route_changes()
        {
            var match = CloneValidMatch(DatabaseFixture.TestData.Matches.First(x => x.StartTime < DateTimeOffset.UtcNow &&
                                                                                    x.Teams.Count == 2 &&
                                                                                    x.MatchResultType is null));

            var updated = await Repository.UpdateStartOfPlay(match, MemberKey, MemberName);

            RedirectsRepository.Verify(x => x.InsertRedirect(match.MatchRoute!, updated.MatchRoute!, null, It.IsAny<IDbTransaction>()));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Saves_match_location(bool resultChanged)
        {
            var matchToUpdate = CloneValidMatch(DatabaseFixture.TestData.Matches.First(x => x.MatchResultType == MatchResultType.HomeWin && x.MatchLocation is not null));
            if (resultChanged)
            {
                matchToUpdate.MatchResultType = MatchResultType.AwayWinByForfeit;
            }
            else
            {
                matchToUpdate.MatchResultType = null;
            }

            var locationBefore = matchToUpdate.MatchLocation!.MatchLocationId;
            matchToUpdate.MatchLocation = DatabaseFixture.TestData.MatchLocations.First(x => x.MatchLocationId != locationBefore);
            var locationAfter = matchToUpdate.MatchLocation.MatchLocationId;

            var result = await Repository.UpdateStartOfPlay(matchToUpdate, MemberKey, MemberName).ConfigureAwait(false);

            Assert.Equal(locationAfter, result.MatchLocation!.MatchLocationId);

            using (var connection = DatabaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var savedMatchLocation = await connection.QuerySingleAsync<Guid>(
                    $"SELECT MatchLocationId FROM {Tables.Match} WHERE MatchId = @MatchId",
                    new
                    {
                        matchToUpdate.MatchId
                    }).ConfigureAwait(false);

                Assert.Equal(locationAfter, savedMatchLocation);
            }
        }

        [Theory]
        [InlineData(MatchResultType.HomeWin, null, true)]
        [InlineData(MatchResultType.HomeWin, null, false)]
        [InlineData(MatchResultType.AwayWin, null, true)]
        [InlineData(MatchResultType.AwayWin, null, false)]
        [InlineData(MatchResultType.Tie, null, true)]
        [InlineData(MatchResultType.Tie, null, false)]
        [InlineData(null, MatchResultType.HomeWinByForfeit, true)]
        [InlineData(null, MatchResultType.HomeWinByForfeit, false)]
        [InlineData(null, MatchResultType.AwayWinByForfeit, true)]
        [InlineData(null, MatchResultType.AwayWinByForfeit, false)]
        [InlineData(null, MatchResultType.Postponed, true)]
        [InlineData(null, MatchResultType.Postponed, false)]
        [InlineData(null, MatchResultType.Cancelled, true)]
        [InlineData(null, MatchResultType.Cancelled, false)]
        [InlineData(null, MatchResultType.AbandonedDuringPlayAndPostponed, true)]
        [InlineData(null, MatchResultType.AbandonedDuringPlayAndPostponed, false)]
        [InlineData(null, MatchResultType.AbandonedDuringPlayAndCancelled, true)]
        [InlineData(null, MatchResultType.AbandonedDuringPlayAndCancelled, false)]

        public async Task Updates_innings_order(MatchResultType? previouslyRecordedResult, MatchResultType? matchResultAfter, bool inningsOrderIsKnownPreviously)
        {
            var inningsOrderIsKnown = !inningsOrderIsKnownPreviously;

            // Two arrangements of result vs null send it down two paths to update InningsOrderIsKnown
            var modifiedMatch = CloneValidMatch(DatabaseFixture.TestData.Matches.First(x => x.StartTime < DateTime.UtcNow &&
                                                                x.MatchResultType == previouslyRecordedResult &&
                                                                x.MatchInnings.Count == 2 &&
                                                                x.InningsOrderIsKnown == inningsOrderIsKnownPreviously));
            modifiedMatch.MatchResultType = matchResultAfter; // null here means "match went ahead" was selected and the actual result should be specified later
            modifiedMatch.InningsOrderIsKnown = inningsOrderIsKnown;

            var previousBattedFirst = modifiedMatch.Teams.SingleOrDefault(t => t.BattedFirst == true)?.TeamRole;
            var modifiedBattedFirst = previousBattedFirst == TeamRole.Home ? TeamRole.Away : TeamRole.Home;
            foreach (var team in modifiedMatch.Teams)
            {
                team.BattedFirst = inningsOrderIsKnown ? team.TeamRole == modifiedBattedFirst : null;
            }

            var result = await Repository.UpdateStartOfPlay(modifiedMatch, MemberKey, MemberName).ConfigureAwait(false);

            Assert.Equal(inningsOrderIsKnown, result.InningsOrderIsKnown);
            if (inningsOrderIsKnown)
            {
                foreach (var innings in result.MatchInnings)
                {
                    var teamRoleOfBattingTeam = result.Teams.Single(t => t.MatchTeamId == innings.BattingMatchTeamId).TeamRole;
                    if (teamRoleOfBattingTeam == modifiedBattedFirst)
                    {
                        Assert.Equal(1, innings.InningsOrderInMatch);
                    }
                    else
                    {
                        Assert.Equal(2, innings.InningsOrderInMatch);
                    }
                }
            }
            else
            {
                foreach (var team in result.Teams)
                {
                    Assert.Null(team.BattedFirst);
                }
            }

            using (var connection = DatabaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var savedOrderKnown = await connection.QuerySingleAsync<bool>(
                    $"SELECT InningsOrderIsKnown FROM {Tables.Match} WHERE MatchId = @MatchId",
                    new
                    {
                        modifiedMatch.MatchId
                    }).ConfigureAwait(false);

                Assert.Equal(inningsOrderIsKnown, savedOrderKnown);

                if (inningsOrderIsKnown)
                {
                    var savedInningsOrder = await connection.QueryAsync<(int InningsOrderInMatch, Guid BattingMatchTeamId)>(
                       $"SELECT InningsOrderInMatch, BattingMatchTeamId FROM {Tables.MatchInnings} WHERE MatchId = @MatchId",
                       new
                       {
                           modifiedMatch.MatchId
                       }).ConfigureAwait(false);

                    foreach (var innings in savedInningsOrder)
                    {
                        var battingTeamRole = result.Teams.Single(t => t.MatchTeamId == innings.BattingMatchTeamId).TeamRole;
                        if (battingTeamRole == modifiedBattedFirst)
                        {
                            Assert.Equal(1, innings.InningsOrderInMatch);
                        }
                        else
                        {
                            Assert.Equal(2, innings.InningsOrderInMatch);
                        }
                    }
                }
            }
        }

        [Fact]
        public async Task Match_that_had_no_teams_assumes_home_team_bats_first()
        {
            var modifiedMatch = FindMatchWithTwoInningsAndNoTeamsAndAddTeams();
            modifiedMatch.InningsOrderIsKnown = false; // prevents home team assumption from being overwritten

            var result = await Repository.UpdateStartOfPlay(modifiedMatch, MemberKey, MemberName).ConfigureAwait(false);

            var firstInnings = result.MatchInnings.OrderBy(x => x.InningsOrderInMatch).First();
            var secondInnings = result.MatchInnings.OrderBy(x => x.InningsOrderInMatch).Skip(1).First();
            var homeTeam = result.Teams.SingleOrDefault(x => x.TeamRole == TeamRole.Home);
            var awayTeam = result.Teams.SingleOrDefault(x => x.TeamRole == TeamRole.Away);

            Assert.NotNull(homeTeam?.MatchTeamId);
            Assert.NotNull(awayTeam?.MatchTeamId);
            Assert.Equal(homeTeam!.MatchTeamId, firstInnings.BattingMatchTeamId);
            Assert.Equal(awayTeam!.MatchTeamId, firstInnings.BowlingMatchTeamId);
            Assert.Equal(homeTeam.MatchTeamId, secondInnings.BowlingMatchTeamId);
            Assert.Equal(awayTeam.MatchTeamId, secondInnings.BattingMatchTeamId);

            using (var connection = DatabaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var savedMatchTeamIds = await connection.QueryAsync<(Guid? MatchTeamId, TeamRole TeamRole)>(
                    $"SELECT MatchTeamId, TeamRole FROM {Tables.MatchTeam} WHERE MatchId = @MatchId",
                    new
                    {
                        modifiedMatch.MatchId
                    }).ConfigureAwait(false);

                var savedInningsOrder = await connection.QueryAsync<(int InningsOrderInMatch, Guid? BattingMatchTeamId, Guid? BowlingMatchTeamId)>(
                    $"SELECT InningsOrderInMatch, BattingMatchTeamId, BowlingMatchTeamId FROM {Tables.MatchInnings} WHERE MatchId = @MatchId",
                    new
                    {
                        modifiedMatch.MatchId
                    }).ConfigureAwait(false);

                var savedFirstInnings = savedInningsOrder.SingleOrDefault(x => x.InningsOrderInMatch == 1);
                var savedSecondInnings = savedInningsOrder.SingleOrDefault(x => x.InningsOrderInMatch == 2);
                var savedHomeTeam = savedMatchTeamIds.SingleOrDefault(x => x.TeamRole == TeamRole.Home);
                var savedAwayTeam = savedMatchTeamIds.SingleOrDefault(x => x.TeamRole == TeamRole.Away);

                Assert.NotNull(savedHomeTeam.MatchTeamId);
                Assert.NotNull(savedAwayTeam.MatchTeamId);
                Assert.Equal(savedHomeTeam!.MatchTeamId, savedFirstInnings.BattingMatchTeamId);
                Assert.Equal(savedAwayTeam!.MatchTeamId, savedFirstInnings.BowlingMatchTeamId);
                Assert.Equal(savedHomeTeam.MatchTeamId, savedSecondInnings.BowlingMatchTeamId);
                Assert.Equal(savedAwayTeam.MatchTeamId, savedSecondInnings.BattingMatchTeamId);
            }
        }

        [Theory]
        [InlineData(MatchResultType.HomeWin, null, true)]
        [InlineData(MatchResultType.HomeWin, null, false)]
        [InlineData(MatchResultType.AwayWin, null, true)]
        [InlineData(MatchResultType.AwayWin, null, false)]
        [InlineData(MatchResultType.Tie, null, true)]
        [InlineData(MatchResultType.Tie, null, false)]
        [InlineData(null, MatchResultType.HomeWinByForfeit, true)]
        [InlineData(null, MatchResultType.HomeWinByForfeit, false)]
        [InlineData(null, MatchResultType.AwayWinByForfeit, true)]
        [InlineData(null, MatchResultType.AwayWinByForfeit, false)]
        [InlineData(null, MatchResultType.Postponed, true)]
        [InlineData(null, MatchResultType.Postponed, false)]
        [InlineData(null, MatchResultType.Cancelled, true)]
        [InlineData(null, MatchResultType.Cancelled, false)]
        [InlineData(null, MatchResultType.AbandonedDuringPlayAndPostponed, true)]
        [InlineData(null, MatchResultType.AbandonedDuringPlayAndPostponed, false)]
        [InlineData(null, MatchResultType.AbandonedDuringPlayAndCancelled, true)]
        [InlineData(null, MatchResultType.AbandonedDuringPlayAndCancelled, false)]
        public async Task Updates_or_preserves_match_name_depending_on_passed_UpdateMatchNameAutomatically_setting(MatchResultType? previouslyRecordedResult, MatchResultType? matchResultAfter, bool updateMatchNameAutomatically)
        {
            var expectUpdatedMatchName = previouslyRecordedResult is null && updateMatchNameAutomatically;

            // Two arrangements of result vs null send it down two paths, only one of which can update match name depending on UpdateMatchNameAutomatically
            var modifiedMatch = CloneValidMatch(DatabaseFixture.TestData.Matches.First(x => x.StartTime < DateTime.UtcNow &&
                                                                            x.MatchResultType == previouslyRecordedResult &&
                                                                            x.UpdateMatchNameAutomatically == updateMatchNameAutomatically));
            modifiedMatch.MatchResultType = matchResultAfter; // null here means "match went ahead" was selected and the actual result should be specified later

            var nameBefore = modifiedMatch.MatchName;

            var result = await Repository.UpdateStartOfPlay(modifiedMatch, MemberKey, MemberName).ConfigureAwait(false);

            Assert.Equal(expectUpdatedMatchName ? UPDATED_MATCH_NAME : nameBefore, result.MatchName);

            using (var connection = DatabaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var savedMatchName = await connection.QuerySingleAsync<string>(
                    $"SELECT MatchName FROM {Tables.Match} WHERE MatchId = @MatchId",
                    new
                    {
                        modifiedMatch.MatchId
                    }).ConfigureAwait(false);

                Assert.Equal(expectUpdatedMatchName ? UPDATED_MATCH_NAME : nameBefore, savedMatchName);
            }
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public async Task Saves_winner_of_toss(bool homeTeamWonToss, bool matchHasTeams)
        {
            var expectedTeams = matchHasTeams ? 2 : 0;
            Stoolball.Matches.Match? modifiedMatch = null;

            if (!matchHasTeams)
            {
                modifiedMatch = FindMatchWithTwoInningsAndNoTeamsAndAddTeams();
            }
            else
            {
                modifiedMatch = CloneValidMatch(DatabaseFixture.TestData.Matches.First(
                                                        x => x.StartTime < DateTime.UtcNow &&
                                                        x.Teams.Count(t => t.TeamRole == TeamRole.Home && t.WonToss is null) == 1 &&
                                                        x.Teams.Count(t => t.TeamRole == TeamRole.Away) == 1));
            }

            var homeTeamInMatch = modifiedMatch.Teams.First(t => t.TeamRole == TeamRole.Home);
            homeTeamInMatch.WonToss = homeTeamWonToss;
            var awayTeamInMatch = modifiedMatch.Teams.First(t => t.TeamRole == TeamRole.Away);
            awayTeamInMatch.WonToss = !homeTeamWonToss;

            var result = await Repository.UpdateStartOfPlay(modifiedMatch, MemberKey, MemberName).ConfigureAwait(false);

            Assert.Equal(homeTeamWonToss, result.Teams.SingleOrDefault(t => t.TeamRole == TeamRole.Home)?.WonToss);
            Assert.Equal(!homeTeamWonToss, result.Teams.SingleOrDefault(t => t.TeamRole == TeamRole.Away)?.WonToss);

            using (var connection = DatabaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var savedToss = await connection.QueryAsync<(TeamRole TeamRole, bool? WonToss)>(
                    $"SELECT TeamRole, WonToss FROM {Tables.MatchTeam} WHERE MatchId = @MatchId",
                    new
                    {
                        modifiedMatch.MatchId
                    }).ConfigureAwait(false);

                Assert.Equal(homeTeamWonToss, savedToss.SingleOrDefault(t => t.TeamRole == TeamRole.Home).WonToss);
                Assert.Equal(!homeTeamWonToss, savedToss.SingleOrDefault(t => t.TeamRole == TeamRole.Away).WonToss);
            }
        }

        private Stoolball.Matches.Match FindMatchWithTwoInningsAndNoTeamsAndAddTeams()
        {
            var match = DatabaseFixture.TestData.Matches.First(x => x.StartTime < DateTime.UtcNow &&
                                                                    x.MatchInnings.Count == 2 &&
                                                                    !x.Teams.Any());
            var modifiedMatch = CloneValidMatch(match);

            // Add teams in match without a MatchTeamId to test INSERT path
            modifiedMatch.Teams.Add(new TeamInMatch
            {
                TeamRole = TeamRole.Home,
                Team = DatabaseFixture.TestData.Teams.First()
            });
            modifiedMatch.Teams.Add(new TeamInMatch
            {
                TeamRole = TeamRole.Away,
                Team = DatabaseFixture.TestData.Teams.First(t => t.TeamId != modifiedMatch.Teams[0].Team!.TeamId)
            });
            return modifiedMatch;
        }

        [Theory]
        [InlineData(MatchResultType.HomeWin, null)]
        [InlineData(MatchResultType.AwayWin, null)]
        [InlineData(MatchResultType.Tie, null)]
        [InlineData(null, MatchResultType.HomeWinByForfeit)]
        [InlineData(null, MatchResultType.AwayWinByForfeit)]
        [InlineData(null, MatchResultType.Postponed)]
        [InlineData(null, MatchResultType.Cancelled)]
        [InlineData(null, MatchResultType.AbandonedDuringPlayAndPostponed)]
        [InlineData(null, MatchResultType.AbandonedDuringPlayAndCancelled)]
        public async Task Saves_match_result(MatchResultType? previouslyRecordedResult, MatchResultType? matchResultAfter)
        {
            var modifiedMatch = CloneValidMatch(DatabaseFixture.TestData.Matches.First(x => x.StartTime < DateTimeOffset.UtcNow && x.MatchResultType == previouslyRecordedResult));
            modifiedMatch.MatchResultType = matchResultAfter; // null here means "match went ahead" was selected and the actual result should be specified later

            var result = await Repository.UpdateStartOfPlay(modifiedMatch, MemberKey, MemberName).ConfigureAwait(false);

            Assert.Equal(matchResultAfter, result.MatchResultType);

            using (var connection = DatabaseFixture.ConnectionFactory.CreateDatabaseConnection())
            {
                var savedMatchResult = await connection.QuerySingleAsync<string>(
                    $"SELECT MatchResultType FROM {Tables.Match} WHERE MatchId = @MatchId",
                    new
                    {
                        modifiedMatch.MatchId
                    }).ConfigureAwait(false);

                Assert.Equal(matchResultAfter is null ? previouslyRecordedResult : matchResultAfter, Enum.Parse(typeof(MatchResultType), savedMatchResult));
            }
        }

        [Fact]
        public async Task Updates_player_statistics()
        {
            var matchToUpdate = CloneValidMatch(DatabaseFixture.TestData.Matches.First(x => x.StartTime < DateTimeOffset.UtcNow && x.MatchResultType is null));

            _ = await Repository.UpdateStartOfPlay(matchToUpdate, MemberKey, MemberName).ConfigureAwait(false);

            StatisticsRepository.Verify(x => x.UpdatePlayerStatistics(It.IsAny<IEnumerable<PlayerInMatchStatisticsRecord>>(), It.IsAny<IDbTransaction>()), Times.Once());
        }
    }
}
