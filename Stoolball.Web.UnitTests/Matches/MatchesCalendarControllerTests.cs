using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Stoolball.Clubs;
using Stoolball.Competitions;
using Stoolball.Data.Abstractions;
using Stoolball.Dates;
using Stoolball.MatchLocations;
using Stoolball.Matches;
using Stoolball.Teams;
using Stoolball.Web.Matches;
using Stoolball.Web.Matches.Models;
using Xunit;

namespace Stoolball.Web.UnitTests.Matches
{
    public class MatchesCalendarControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IMatchFilterQueryStringParser> _matchFilterQueryStringParser = new();
        private readonly Mock<IMatchFilterHumanizer> _matchFilterHumanizer = new();
        private readonly Mock<IClubDataSource> _clubDataSource = new();
        private readonly Mock<ITeamDataSource> _teamDataSource = new();
        private readonly Mock<ICompetitionDataSource> _competitionDataSource = new();
        private readonly Mock<IMatchLocationDataSource> _matchLocationDataSource = new();
        private readonly Mock<ITournamentDataSource> _tournamentDataSource = new();
        private readonly Mock<IMatchListingDataSource> _matchListingDataSource = new();
        private readonly Mock<IMatchDataSource> _matchDataSource = new();

        private MatchesCalendarController CreateController()
        {
            return new MatchesCalendarController(
                Mock.Of<ILogger<MatchesCalendarController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _clubDataSource.Object,
                _teamDataSource.Object,
                _competitionDataSource.Object,
                _matchLocationDataSource.Object,
                _matchListingDataSource.Object,
                _tournamentDataSource.Object,
                _matchDataSource.Object,
                Mock.Of<IDateTimeFormatter>(),
                _matchFilterQueryStringParser.Object,
                _matchFilterHumanizer.Object)
            {
                ControllerContext = ControllerContext
            };
        }

        [Fact]
        public async Task No_matches_returns_MatchListingViewModel()
        {
            var filter = new MatchFilter();

            _matchListingDataSource.Setup(x => x.ReadMatchListings(filter, MatchSortOrder.LatestUpdateFirst)).ReturnsAsync(new List<MatchListing>());

            _matchFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<MatchFilter>(), It.IsAny<string>())).Returns(filter);

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<MatchListingViewModel>(((ViewResult)result).Model);
            }
        }

        private void SetupPath(string path) => Request.SetupGet(x => x.Path).Returns(new PathString(path));

        private MatchFilter SetupFilter(DateTimeOffset? fromDate = null)
        {
            var filter = new MatchFilter { FromDate = fromDate ?? DateTimeOffset.UtcNow.Date };
            _matchFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<MatchFilter>(), It.IsAny<string>())).Returns(filter);
            _matchListingDataSource.Setup(x => x.ReadMatchListings(filter, MatchSortOrder.LatestUpdateFirst)).ReturnsAsync(new List<MatchListing>());
            return filter;
        }

        private async Task<IActionResult> GetResult()
        {
            using var controller = CreateController();
            return await controller.Index();
        }

        private async Task<MatchListingViewModel> GetModel() => (MatchListingViewModel)((ViewResult)await GetResult()).Model!;

        [Fact]
        public async Task Default_title_is_matches_and_tournaments()
        {
            SetupFilter();

            var model = await GetModel();

            Assert.Equal("Stoolball matches and tournaments", model.Metadata.PageTitle);
        }

        [Fact]
        public async Task Default_filter_includes_matches_and_tournaments_from_today()
        {
            MatchFilter? defaultFilter = null;
            _matchFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<MatchFilter>(), It.IsAny<string>()))
                .Callback<MatchFilter, string>((f, _) => defaultFilter = f)
                .Returns(new MatchFilter());

            await GetResult();

            Assert.True(defaultFilter!.IncludeMatches);
            Assert.True(defaultFilter.IncludeTournaments);
            Assert.False(defaultFilter.IncludeTournamentMatches);
            Assert.Equal(DateTimeOffset.UtcNow.Date, defaultFilter.FromDate);
        }

        [Fact]
        public async Task From_date_in_the_past_is_reset_to_today()
        {
            var filter = SetupFilter(DateTimeOffset.UtcNow.Date.AddDays(-30));

            await GetResult();

            Assert.Equal(DateTimeOffset.UtcNow.Date, filter.FromDate);
        }

        [Fact]
        public async Task From_date_in_the_future_is_kept()
        {
            var future = DateTimeOffset.UtcNow.Date.AddDays(30);
            var filter = SetupFilter(future);

            await GetResult();

            Assert.Equal(future, filter.FromDate);
        }

        [Fact]
        public async Task Default_from_date_is_removed_from_filter_description()
        {
            SetupFilter();
            MatchFilter? humanizedFilter = null;
            _matchFilterHumanizer.Setup(x => x.MatchingFilter(It.IsAny<MatchFilter>())).Callback<MatchFilter>(f => humanizedFilter = f).Returns(string.Empty);

            await GetResult();

            Assert.Null(humanizedFilter!.FromDate);
        }

        [Fact]
        public async Task Custom_from_date_is_kept_in_filter_description()
        {
            var future = DateTimeOffset.UtcNow.Date.AddDays(30);
            SetupFilter(future);
            MatchFilter? humanizedFilter = null;
            _matchFilterHumanizer.Setup(x => x.MatchingFilter(It.IsAny<MatchFilter>())).Callback<MatchFilter>(f => humanizedFilter = f).Returns(string.Empty);

            await GetResult();

            Assert.Equal(future, humanizedFilter!.FromDate);
        }

        [Fact]
        public async Task Humanized_filter_is_appended_to_title()
        {
            SetupFilter();
            _matchFilterHumanizer.Setup(x => x.MatchingFilter(It.IsAny<MatchFilter>())).Returns(" matching filter");

            var model = await GetModel();

            Assert.Equal("Stoolball matches and tournaments matching filter", model.Metadata.PageTitle);
        }

        [Fact]
        public async Task Matches_ics_route_excludes_tournaments()
        {
            var filter = SetupFilter();
            SetupPath("/matches/ics");

            var model = await GetModel();

            Assert.False(filter.IncludeTournaments);
            Assert.Equal("Stoolball matches", model.Metadata.PageTitle);
        }

        [Fact]
        public async Task Tournaments_ics_route_excludes_matches()
        {
            var filter = SetupFilter();
            SetupPath("/tournaments.ics");

            var model = await GetModel();

            Assert.False(filter.IncludeMatches);
            Assert.Equal("Stoolball tournaments", model.Metadata.PageTitle);
            Assert.Empty(filter.PlayerTypes);
        }

        [Theory]
        [InlineData("all", new PlayerType[0], "Stoolball tournaments")]
        [InlineData("mixed", new[] { PlayerType.Mixed }, "Mixed stoolball tournaments")]
        [InlineData("ladies", new[] { PlayerType.Ladies }, "Ladies stoolball tournaments")]
        [InlineData("junior", new[] { PlayerType.JuniorMixed, PlayerType.JuniorGirls, PlayerType.JuniorBoys }, "Junior stoolball tournaments")]
        public async Task Legacy_tournaments_calendar_route_filters_by_player_type(string legacySegment, PlayerType[] expectedPlayerTypes, string expectedTitle)
        {
            var filter = SetupFilter();
            SetupPath($"/tournaments/{legacySegment}/calendar.ics");

            var model = await GetModel();

            Assert.False(filter.IncludeMatches);
            Assert.Equal(expectedPlayerTypes, filter.PlayerTypes);
            Assert.Equal(expectedTitle, model.Metadata.PageTitle);
        }

        [Fact]
        public async Task Tournament_route_shows_only_that_tournament_and_does_not_read_listings()
        {
            SetupFilter();
            var tournament = new Tournament { TournamentId = Guid.NewGuid(), TournamentName = "Example tournament", StartTime = new DateTimeOffset(2030, 5, 1, 10, 0, 0, TimeSpan.Zero), StartTimeIsKnown = true };
            SetupPath("/tournaments/example/calendar.ics");
            _tournamentDataSource.Setup(x => x.ReadTournamentByRoute(It.IsAny<string>())).ReturnsAsync(tournament);

            var model = await GetModel();

            Assert.Single(model.Matches);
            Assert.Equal(tournament.TournamentId, model.Matches[0].MatchId);
            Assert.StartsWith("Example tournament", model.Metadata.PageTitle);
            _matchListingDataSource.Verify(x => x.ReadMatchListings(It.IsAny<MatchFilter>(), It.IsAny<MatchSortOrder>()), Times.Never);
        }

        [Fact]
        public async Task Tournament_route_not_matching_tournament_returns_404()
        {
            SetupFilter();
            SetupPath("/tournaments/example/calendar.ics");
            _tournamentDataSource.Setup(x => x.ReadTournamentByRoute(It.IsAny<string>())).Returns(Task.FromResult<Tournament>(null!));

            Assert.IsType<NotFoundResult>(await GetResult());
        }

        [Fact]
        public async Task Match_route_shows_only_that_match_and_does_not_read_listings()
        {
            SetupFilter();
            var match = new Stoolball.Matches.Match { MatchId = Guid.NewGuid(), MatchName = "Team A v Team B", StartTime = new DateTimeOffset(2030, 5, 1, 10, 0, 0, TimeSpan.Zero), StartTimeIsKnown = true };
            SetupPath("/matches/example/calendar.ics");
            _matchDataSource.Setup(x => x.ReadMatchByRoute(It.IsAny<string>())).ReturnsAsync(match);

            var model = await GetModel();

            Assert.Single(model.Matches);
            Assert.Equal(match.MatchId, model.Matches[0].MatchId);
            Assert.StartsWith("Team A v Team B", model.Metadata.PageTitle);
            _matchListingDataSource.Verify(x => x.ReadMatchListings(It.IsAny<MatchFilter>(), It.IsAny<MatchSortOrder>()), Times.Never);
        }

        [Fact]
        public async Task Match_route_not_matching_match_returns_404()
        {
            SetupFilter();
            SetupPath("/matches/example/calendar.ics");
            _matchDataSource.Setup(x => x.ReadMatchByRoute(It.IsAny<string>())).Returns(Task.FromResult<Stoolball.Matches.Match>(null!));

            Assert.IsType<NotFoundResult>(await GetResult());
        }

        [Fact]
        public async Task Club_route_adds_club_teams_to_filter_and_name_to_title()
        {
            var filter = SetupFilter();
            var team1 = Guid.NewGuid();
            var team2 = Guid.NewGuid();
            var club = new Club { ClubName = "Example club" };
            club.Teams.Add(new Team { TeamId = team1 });
            club.Teams.Add(new Team { TeamId = team2 });
            SetupPath("/clubs/example/calendar.ics");
            _clubDataSource.Setup(x => x.ReadClubByRoute(It.IsAny<string>())).ReturnsAsync(club);

            var model = await GetModel();

            Assert.Equal("Stoolball matches and tournaments for Example club", model.Metadata.PageTitle);
            Assert.Equal(new[] { team1, team2 }, filter.TeamIds);
        }

        [Fact]
        public async Task Club_route_not_matching_club_returns_404()
        {
            SetupFilter();
            SetupPath("/clubs/example/calendar.ics");
            _clubDataSource.Setup(x => x.ReadClubByRoute(It.IsAny<string>())).Returns(Task.FromResult<Club?>(null));

            Assert.IsType<NotFoundResult>(await GetResult());
        }

        [Fact]
        public async Task Team_route_adds_team_to_filter_and_name_to_title()
        {
            var filter = SetupFilter();
            var teamId = Guid.NewGuid();
            SetupPath("/teams/example/calendar.ics");
            _teamDataSource.Setup(x => x.ReadTeamByRoute(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(new Team { TeamId = teamId, TeamName = "Example team" });

            var model = await GetModel();

            Assert.Equal("Stoolball matches and tournaments for Example team", model.Metadata.PageTitle);
            Assert.Equal(new[] { teamId }, filter.TeamIds);
        }

        [Fact]
        public async Task Team_route_not_matching_team_returns_404()
        {
            SetupFilter();
            SetupPath("/teams/example/calendar.ics");
            _teamDataSource.Setup(x => x.ReadTeamByRoute(It.IsAny<string>(), It.IsAny<bool>())).Returns(Task.FromResult<Team?>(null));

            Assert.IsType<NotFoundResult>(await GetResult());
        }

        [Fact]
        public async Task Competition_route_adds_competition_to_filter_and_name_to_title()
        {
            var filter = SetupFilter();
            var competitionId = Guid.NewGuid();
            SetupPath("/competitions/example/calendar.ics");
            _competitionDataSource.Setup(x => x.ReadCompetitionByRoute(It.IsAny<string>())).ReturnsAsync(new Competition { CompetitionId = competitionId, CompetitionName = "Example league" });

            var model = await GetModel();

            Assert.Equal("Stoolball matches and tournaments in the Example league", model.Metadata.PageTitle);
            Assert.Equal(new[] { competitionId }, filter.CompetitionIds);
        }

        [Fact]
        public async Task Competition_route_not_matching_competition_returns_404()
        {
            SetupFilter();
            SetupPath("/competitions/example/calendar.ics");
            _competitionDataSource.Setup(x => x.ReadCompetitionByRoute(It.IsAny<string>())).Returns(Task.FromResult<Competition?>(null));

            Assert.IsType<NotFoundResult>(await GetResult());
        }

        [Fact]
        public async Task Location_route_adds_location_to_filter_and_name_to_title()
        {
            var filter = SetupFilter();
            var locationId = Guid.NewGuid();
            var location = new MatchLocation { MatchLocationId = locationId, PrimaryAddressableObjectName = "Example ground", Town = "Exampletown" };
            SetupPath("/locations/example/calendar.ics");
            _matchLocationDataSource.Setup(x => x.ReadMatchLocationByRoute(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(location);

            var model = await GetModel();

            Assert.Equal("Stoolball matches and tournaments at " + location.NameAndLocalityOrTown(), model.Metadata.PageTitle);
            Assert.Equal(new[] { locationId }, filter.MatchLocationIds);
        }

        [Fact]
        public async Task Location_route_not_matching_location_returns_404()
        {
            SetupFilter();
            SetupPath("/locations/example/calendar.ics");
            _matchLocationDataSource.Setup(x => x.ReadMatchLocationByRoute(It.IsAny<string>(), It.IsAny<bool>())).Returns(Task.FromResult<MatchLocation?>(null));

            Assert.IsType<NotFoundResult>(await GetResult());
        }

        [Fact]
        public async Task Listings_are_read_latest_update_first_for_list_routes()
        {
            var filter = SetupFilter();
            var listings = new List<MatchListing> { new MatchListing() };
            _matchListingDataSource.Setup(x => x.ReadMatchListings(filter, MatchSortOrder.LatestUpdateFirst)).ReturnsAsync(listings);
            SetupPath("/matches/ics");

            var model = await GetModel();

            Assert.Same(listings, model.Matches);
        }
    }
}
