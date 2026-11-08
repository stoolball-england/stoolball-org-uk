using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Stoolball.Competitions;
using Stoolball.Data.Abstractions;
using Stoolball.Email;
using Stoolball.Matches;
using Stoolball.Security;
using Stoolball.Teams;
using Stoolball.Web.Competitions;
using Stoolball.Web.Competitions.Models;
using Xunit;

namespace Stoolball.Web.UnitTests.Competitions
{
    public class SeasonResultsTableControllerTests : UmbracoBaseTest
    {
        private readonly Mock<ISeasonDataSource> _seasonDataSource = new();
        private readonly Mock<IMatchListingDataSource> _matchListingDataSource = new();
        private readonly Mock<IAuthorizationPolicy<Competition>> _authorizationPolicy = new();
        private readonly Mock<IEmailProtector> _emailProtector = new();

        private SeasonResultsTableController CreateController()
        {
            return new SeasonResultsTableController(
                Mock.Of<ILogger<SeasonResultsTableController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _seasonDataSource.Object,
                _matchListingDataSource.Object,
                _authorizationPolicy.Object,
                _emailProtector.Object
                )
            {
                ControllerContext = ControllerContext
            };
        }

        [Fact]
        public async Task Route_not_matching_season_returns_404()
        {
            _seasonDataSource.Setup(x => x.ReadSeasonByRoute(It.IsAny<string>(), true)).Returns(Task.FromResult<Season?>(null));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<NotFoundResult>(result);
            }
        }


        [Fact]
        public async Task Route_matching_season_without_results_returns_404()
        {
            _seasonDataSource.Setup(x => x.ReadSeasonByRoute(It.IsAny<string>(), true)).ReturnsAsync(new Season { Competition = new Competition { CompetitionName = "Example" } });

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<NotFoundResult>(result);
            }
        }

        [Fact]
        public async Task Route_matching_season_with_training_sessions_returns_404()
        {
            _seasonDataSource.Setup(x => x.ReadSeasonByRoute(It.IsAny<string>(), true)).ReturnsAsync(new Season
            {
                Competition = new Competition { CompetitionName = "Example" },
                MatchTypes = new List<MatchType>() { MatchType.TrainingSession }
            });

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<NotFoundResult>(result);
            }
        }

        [Fact]
        public async Task Route_matching_season_with_results_returns_SeasonViewModel()
        {
            var season = new Season
            {
                SeasonId = Guid.NewGuid(),
                Competition = new Competition
                {
                    CompetitionName = "Example",
                    CompetitionRoute = "/competitions/example"
                },
                Results = "Example",
                SeasonRoute = "/competitions/example/1234"
            };
            _seasonDataSource.Setup(x => x.ReadSeasonByRoute(It.IsAny<string>(), true)).ReturnsAsync(season);

            _matchListingDataSource.Setup(x => x.ReadMatchListings(
                It.Is<MatchFilter>(mf => mf.SeasonIds.Count == 1 && mf.SeasonIds[0] == season.SeasonId && mf.IncludeTournaments == false),
                MatchSortOrder.MatchDateEarliestFirst)).ReturnsAsync(new List<MatchListing>());

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<SeasonViewModel>(((ViewResult)result).Model);
            }
        }

        [Fact]
        public async Task Route_matching_season_with_league_matches_returns_SeasonViewModel()
        {
            var season = new Season
            {
                SeasonId = Guid.NewGuid(),
                Competition = new Competition
                {
                    CompetitionName = "Example",
                    CompetitionRoute = "/competitions/example"
                },
                MatchTypes = new List<MatchType>() { MatchType.LeagueMatch },
                SeasonRoute = "/competitions/example/1234"
            };
            _seasonDataSource.Setup(x => x.ReadSeasonByRoute(It.IsAny<string>(), true)).ReturnsAsync(season);

            _matchListingDataSource.Setup(x => x.ReadMatchListings(
                It.Is<MatchFilter>(mf => mf.SeasonIds.Count == 1 && mf.SeasonIds[0] == season.SeasonId && mf.IncludeTournaments == false),
                MatchSortOrder.MatchDateEarliestFirst)).ReturnsAsync(new List<MatchListing>());

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<SeasonViewModel>(((ViewResult)result).Model);
            }
        }

        private static readonly Team _homeTeam = new() { TeamId = Guid.NewGuid(), TeamName = "Home team" };
        private static readonly Team _awayTeam = new() { TeamId = Guid.NewGuid(), TeamName = "Away team" };

        private Season SetupSeason(ResultsTableType tableType = ResultsTableType.LeagueTable, string competitionName = "Example league", params MatchListing[] matches)
        {
            var season = new Season
            {
                SeasonId = Guid.NewGuid(),
                FromYear = 2026,
                UntilYear = 2026,
                Competition = new Competition { CompetitionName = competitionName, CompetitionRoute = "/competitions/example" },
                SeasonRoute = "/competitions/example/2026",
                ResultsTableType = tableType
            };
            season.MatchTypes.Add(MatchType.LeagueMatch);
            season.Teams.Add(new TeamInSeason { Team = _homeTeam });
            season.Teams.Add(new TeamInSeason { Team = _awayTeam });
            _seasonDataSource.Setup(x => x.ReadSeasonByRoute(It.IsAny<string>(), true)).ReturnsAsync(season);
            _seasonDataSource.Setup(x => x.ReadPointsRules(season.SeasonId.Value)).ReturnsAsync(
            [
                new PointsRule { MatchResultType = MatchResultType.HomeWin, HomePoints = 3, AwayPoints = 0 },
                new PointsRule { MatchResultType = MatchResultType.AwayWin, HomePoints = 0, AwayPoints = 3 },
                new PointsRule { MatchResultType = MatchResultType.Tie, HomePoints = 1, AwayPoints = 1 }
            ]);
            _seasonDataSource.Setup(x => x.ReadPointsAdjustments(season.SeasonId.Value)).ReturnsAsync(new List<PointsAdjustment>());
            _matchListingDataSource.Setup(x => x.ReadMatchListings(It.IsAny<MatchFilter>(), MatchSortOrder.MatchDateEarliestFirst)).ReturnsAsync(matches.ToList());
            return season;
        }

        private static MatchListing CreateMatch(MatchResultType? result, MatchType matchType = MatchType.LeagueMatch, int daysAgo = 7, int? homeRuns = null, int? awayRuns = null)
        {
            var homeMatchTeamId = Guid.NewGuid();
            var awayMatchTeamId = Guid.NewGuid();
            var match = new MatchListing
            {
                MatchId = Guid.NewGuid(),
                StartTime = DateTimeOffset.UtcNow.AddDays(-daysAgo),
                MatchType = matchType,
                MatchResultType = result
            };
            match.Teams.Add(new TeamInMatch { MatchTeamId = homeMatchTeamId, Team = _homeTeam, TeamRole = TeamRole.Home });
            match.Teams.Add(new TeamInMatch { MatchTeamId = awayMatchTeamId, Team = _awayTeam, TeamRole = TeamRole.Away });
            match.MatchInnings.Add(new MatchInnings { BattingMatchTeamId = homeMatchTeamId, Runs = homeRuns });
            match.MatchInnings.Add(new MatchInnings { BattingMatchTeamId = awayMatchTeamId, Runs = awayRuns });
            return match;
        }

        private async Task<SeasonViewModel> ReadModel()
        {
            using var controller = CreateController();

            var result = await controller.Index();

            return Assert.IsType<SeasonViewModel>(((ViewResult)result).Model);
        }

        private static ResultsTableRow RowFor(SeasonViewModel model, Team team)
        {
            return ((SeasonResultsTableViewModel)model.Matches).ResultsTableRows.Single(x => x.Team == team);
        }

        [Fact]
        public async Task Route_matching_season_with_only_friendly_matches_returns_SeasonViewModel()
        {
            var season = SetupSeason();
            season.MatchTypes.Clear();
            season.MatchTypes.Add(MatchType.FriendlyMatch);

            var model = await ReadModel();

            Assert.Equal(season, model.Season);
        }

        [Fact]
        public async Task Points_rules_and_adjustments_are_read_for_the_season()
        {
            var season = SetupSeason();
            var adjustment = new PointsAdjustment { Team = _homeTeam, Points = -1 };
            _seasonDataSource.Setup(x => x.ReadPointsAdjustments(season.SeasonId!.Value)).ReturnsAsync([adjustment]);

            var model = await ReadModel();

            Assert.Equal(3, model.Season!.PointsRules.Count);
            Assert.Equal([adjustment], model.Season.PointsAdjustments);
        }

        [Fact]
        public async Task Email_addresses_in_results_are_protected_for_signed_out_user()
        {
            var season = SetupSeason();
            season.Results = "<p>results@example.org</p>";
            _emailProtector.Setup(x => x.ProtectEmailAddresses("<p>results@example.org</p>", false)).Returns("<p>protected results</p>");

            var model = await ReadModel();

            Assert.Equal("<p>protected results</p>", model.Season!.Results);
        }

        [Fact]
        public async Task Model_has_authorization()
        {
            var season = SetupSeason();
            var authorization = new Dictionary<AuthorizedAction, bool> { { AuthorizedAction.EditCompetition, true } };
            _authorizationPolicy.Setup(x => x.IsAuthorized(season.Competition!)).ReturnsAsync(authorization);

            var model = await ReadModel();

            Assert.Equal(authorization, model.Authorization.CurrentMemberIsAuthorized);
        }

        [Fact]
        public async Task Model_has_page_title_description_and_breadcrumbs()
        {
            var season = SetupSeason();

            var model = await ReadModel();

            Assert.Equal($"Results table for the {season.SeasonFullNameAndPlayerType()}", model.Metadata.PageTitle);
            Assert.Equal(season.Description(), model.Metadata.Description);
            Assert.Equal([Constants.Pages.Competitions, "Example league"], model.Breadcrumbs.Select(x => x.Name).TakeLast(2));
        }

        [Fact]
        public async Task Page_title_does_not_repeat_the_when_competition_name_starts_with_it()
        {
            var season = SetupSeason(competitionName: "The Example league");

            var model = await ReadModel();

            Assert.Equal($"Results table for {season.SeasonFullNameAndPlayerType()}", model.Metadata.PageTitle);
        }

        [Fact]
        public async Task Every_team_not_withdrawn_has_a_row_even_without_matches()
        {
            SetupSeason();

            var model = await ReadModel();

            var rows = ((SeasonResultsTableViewModel)model.Matches).ResultsTableRows.ToList();
            Assert.Equal(2, rows.Count);
            Assert.All(rows, row => Assert.Equal(0, row.Played));
        }

        [Fact]
        public async Task Withdrawn_team_has_no_row_and_its_matches_are_ignored()
        {
            var season = SetupSeason(matches: CreateMatch(MatchResultType.HomeWin));
            season.Teams.Single(x => x.Team == _awayTeam).WithdrawnDate = DateTimeOffset.UtcNow.AddDays(-30);

            var model = await ReadModel();

            var row = Assert.Single(((SeasonResultsTableViewModel)model.Matches).ResultsTableRows);
            Assert.Equal(_homeTeam, row.Team);
            Assert.Equal(0, row.Played);
        }

        [Fact]
        public async Task Home_win_awards_points_and_updates_won_lost_and_played()
        {
            SetupSeason(matches: CreateMatch(MatchResultType.HomeWin));

            var model = await ReadModel();

            var home = RowFor(model, _homeTeam);
            var away = RowFor(model, _awayTeam);
            Assert.Equal((1, 1, 0, 3), (home.Played, home.Won, home.Lost, home.Points));
            Assert.Equal((1, 0, 1, 0), (away.Played, away.Won, away.Lost, away.Points));
        }

        [Fact]
        public async Task Away_win_awards_points_and_updates_won_lost_and_played()
        {
            SetupSeason(matches: CreateMatch(MatchResultType.AwayWin));

            var model = await ReadModel();

            var home = RowFor(model, _homeTeam);
            var away = RowFor(model, _awayTeam);
            Assert.Equal((1, 0, 1, 0), (home.Played, home.Won, home.Lost, home.Points));
            Assert.Equal((1, 1, 0, 3), (away.Played, away.Won, away.Lost, away.Points));
        }

        [Fact]
        public async Task Tie_updates_tied_and_awards_points_to_both_teams()
        {
            SetupSeason(matches: CreateMatch(MatchResultType.Tie));

            var model = await ReadModel();

            var home = RowFor(model, _homeTeam);
            var away = RowFor(model, _awayTeam);
            Assert.Equal((1, 1, 1), (home.Played, home.Tied, home.Points));
            Assert.Equal((1, 1, 1), (away.Played, away.Tied, away.Points));
        }

        [Fact]
        public async Task Cancelled_match_counts_as_no_result()
        {
            var season = SetupSeason(matches: CreateMatch(MatchResultType.Cancelled));
            _seasonDataSource.Setup(x => x.ReadPointsRules(season.SeasonId!.Value)).ReturnsAsync([new PointsRule { MatchResultType = MatchResultType.Cancelled }]);

            var model = await ReadModel();

            Assert.Equal((1, 1), (RowFor(model, _homeTeam).Played, RowFor(model, _homeTeam).NoResult));
            Assert.Equal((1, 1), (RowFor(model, _awayTeam).Played, RowFor(model, _awayTeam).NoResult));
        }

        [Fact]
        public async Task Postponed_matches_are_ignored()
        {
            SetupSeason(matches: CreateMatch(MatchResultType.Postponed));

            var model = await ReadModel();

            Assert.Equal(0, RowFor(model, _homeTeam).Played);
            Assert.Empty(((SeasonResultsTableViewModel)model.Matches).MatchesAwaitingResults);
        }

        [Fact]
        public async Task Future_matches_are_ignored()
        {
            SetupSeason(matches: CreateMatch(null, daysAgo: -7));

            var model = await ReadModel();

            Assert.Equal(0, RowFor(model, _homeTeam).Played);
            Assert.Empty(((SeasonResultsTableViewModel)model.Matches).MatchesAwaitingResults);
        }

        [Fact]
        public async Task Past_match_without_result_is_listed_as_awaiting_result()
        {
            var match = CreateMatch(null);
            SetupSeason(matches: match);

            var model = await ReadModel();

            Assert.Equal(0, RowFor(model, _homeTeam).Played);
            Assert.Equal([match], ((SeasonResultsTableViewModel)model.Matches).MatchesAwaitingResults);
        }

        [Fact]
        public async Task Non_league_matches_are_ignored_in_a_league_table()
        {
            SetupSeason(matches: CreateMatch(MatchResultType.HomeWin, MatchType.FriendlyMatch));

            var model = await ReadModel();

            Assert.Equal(0, RowFor(model, _homeTeam).Played);
        }

        [Fact]
        public async Task Non_league_matches_are_counted_without_points_in_a_non_league_results_table()
        {
            SetupSeason(ResultsTableType.NonLeagueResultsTable, matches: CreateMatch(MatchResultType.HomeWin, MatchType.FriendlyMatch));

            var model = await ReadModel();

            var home = RowFor(model, _homeTeam);
            Assert.Equal((1, 1, 0), (home.Played, home.Won, home.Points));
        }

        [Fact]
        public async Task Runs_are_totalled_when_enabled()
        {
            var season = SetupSeason(matches: CreateMatch(MatchResultType.HomeWin, homeRuns: 100, awayRuns: 80));
            season.EnableRunsScored = true;
            season.EnableRunsConceded = true;

            var model = await ReadModel();

            var home = RowFor(model, _homeTeam);
            var away = RowFor(model, _awayTeam);
            Assert.Equal((100, 80), (home.RunsScored, home.RunsConceded));
            Assert.Equal((80, 100), (away.RunsScored, away.RunsConceded));
        }

        [Fact]
        public async Task Runs_are_not_totalled_when_not_enabled()
        {
            SetupSeason(matches: CreateMatch(MatchResultType.HomeWin, homeRuns: 100, awayRuns: 80));

            var model = await ReadModel();

            var home = RowFor(model, _homeTeam);
            Assert.Equal((0, 0), (home.RunsScored, home.RunsConceded));
        }

        [Fact]
        public async Task Points_adjustments_are_applied_to_a_league_table()
        {
            var season = SetupSeason(matches: CreateMatch(MatchResultType.HomeWin));
            _seasonDataSource.Setup(x => x.ReadPointsAdjustments(season.SeasonId!.Value)).ReturnsAsync([new PointsAdjustment { Team = _homeTeam, Points = -2 }, new PointsAdjustment { Team = _awayTeam, Points = 5 }]);

            var model = await ReadModel();

            Assert.Equal(1, RowFor(model, _homeTeam).Points);
            Assert.Equal(5, RowFor(model, _awayTeam).Points);
        }

        [Fact]
        public async Task Points_adjustments_are_ignored_in_a_non_league_results_table()
        {
            var season = SetupSeason(ResultsTableType.NonLeagueResultsTable, matches: CreateMatch(MatchResultType.HomeWin));
            _seasonDataSource.Setup(x => x.ReadPointsAdjustments(season.SeasonId!.Value)).ReturnsAsync([new PointsAdjustment { Team = _homeTeam, Points = -2 }]);

            var model = await ReadModel();

            Assert.Equal(0, RowFor(model, _homeTeam).Points);
        }
    }
}
