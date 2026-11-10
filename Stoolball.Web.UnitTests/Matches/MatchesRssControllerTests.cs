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
using Stoolball.MatchLocations;
using Stoolball.Matches;
using Stoolball.Teams;
using Stoolball.Web.Matches;
using Stoolball.Web.Matches.Models;
using Xunit;

namespace Stoolball.Web.UnitTests.Matches
{
    public class MatchesRssControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IMatchListingDataSource> _matchDataSource = new();
        private readonly Mock<IMatchesRssQueryStringParser> _queryStringParser = new();
        private readonly Mock<IMatchFilterHumanizer> _matchFilterHumanizer = new();
        private readonly Mock<IClubDataSource> _clubDataSource = new();
        private readonly Mock<ITeamDataSource> _teamDataSource = new();
        private readonly Mock<ICompetitionDataSource> _competitionDataSource = new();
        private readonly Mock<IMatchLocationDataSource> _matchLocationDataSource = new();

        private MatchesRssController CreateController()
        {
            return new MatchesRssController(
                Mock.Of<ILogger<MatchesRssController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _clubDataSource.Object,
                _teamDataSource.Object,
                _competitionDataSource.Object,
                _matchLocationDataSource.Object,
                _matchDataSource.Object,
                _queryStringParser.Object,
                _matchFilterHumanizer.Object)
            {
                ControllerContext = ControllerContext
            };
        }

        [Fact]
        public async Task No_matches_returns_MatchListingViewModel()
        {
            var filter = new MatchFilter();
            _queryStringParser.Setup(x => x.ParseFilterFromQueryString(It.IsAny<string>())).Returns(filter);

            _matchDataSource.Setup(x => x.ReadMatchListings(filter, MatchSortOrder.LatestUpdateFirst)).ReturnsAsync(new List<MatchListing>());

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<MatchListingViewModel>(((ViewResult)result).Model);
            }
        }

        private void SetupPath(string path) => Request.SetupGet(x => x.Path).Returns(new PathString(path));

        private MatchFilter SetupFilter()
        {
            var filter = new MatchFilter();
            _queryStringParser.Setup(x => x.ParseFilterFromQueryString(It.IsAny<string>())).Returns(filter);
            _matchDataSource.Setup(x => x.ReadMatchListings(filter, MatchSortOrder.LatestUpdateFirst)).ReturnsAsync(new List<MatchListing>());
            return filter;
        }

        private async Task<IActionResult> GetResult()
        {
            using var controller = CreateController();
            return await controller.Index();
        }

        private async Task<MatchListingViewModel> GetModel() => (MatchListingViewModel)((ViewResult)await GetResult()).Model!;

        [Fact]
        public async Task Returns_MatchesRss_view()
        {
            SetupFilter();

            var result = (ViewResult)await GetResult();

            Assert.Equal("MatchesRss", result.ViewName);
        }

        [Fact]
        public async Task Default_title_and_description_are_set()
        {
            SetupFilter();

            var model = await GetModel();

            Assert.Equal("Stoolball matches", model.Metadata.PageTitle);
            Assert.Equal("New or updated stoolball matches on the Stoolball England website", model.Metadata.Description);
        }

        [Fact]
        public async Task Matches_are_assigned_to_view_model()
        {
            var filter = SetupFilter();
            var listings = new List<MatchListing> { new MatchListing() };
            _matchDataSource.Setup(x => x.ReadMatchListings(filter, MatchSortOrder.LatestUpdateFirst)).ReturnsAsync(listings);

            var model = await GetModel();

            Assert.Same(listings, model.Matches);
        }

        [Fact]
        public async Task Humanized_filter_is_appended_to_title()
        {
            SetupFilter();
            _matchFilterHumanizer.Setup(x => x.MatchingFilter(It.IsAny<MatchFilter>())).Returns(" matching filter");

            var model = await GetModel();

            Assert.Equal("Stoolball matches matching filter", model.Metadata.PageTitle);
        }

        [Fact]
        public async Task Rolling_until_date_is_removed_from_title_description_but_not_applied_filter()
        {
            var filter = SetupFilter();
            var rollingDate = DateTimeOffset.UtcNow.Date.AddDays(365).AddDays(1).AddSeconds(-1);
            filter.UntilDate = rollingDate;
            MatchFilter? humanizedFilter = null;
            _matchFilterHumanizer.Setup(x => x.MatchingFilter(It.IsAny<MatchFilter>())).Callback<MatchFilter>(f => humanizedFilter = f).Returns(string.Empty);

            var model = await GetModel();

            Assert.Null(humanizedFilter!.UntilDate);
            Assert.Equal(rollingDate, model.AppliedMatchFilter.UntilDate);
        }

        [Fact]
        public async Task Specific_until_date_is_kept_in_title_description()
        {
            var filter = SetupFilter();
            filter.UntilDate = new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);
            MatchFilter? humanizedFilter = null;
            _matchFilterHumanizer.Setup(x => x.MatchingFilter(It.IsAny<MatchFilter>())).Callback<MatchFilter>(f => humanizedFilter = f).Returns(string.Empty);

            await GetModel();

            Assert.Equal(filter.UntilDate, humanizedFilter!.UntilDate);
        }

        [Fact]
        public async Task Player_type_is_added_to_title_and_description()
        {
            var filter = SetupFilter();
            filter.PlayerTypes.Add(PlayerType.Ladies);

            var model = await GetModel();

            Assert.Equal("Ladies stoolball matches", model.Metadata.PageTitle);
            Assert.Equal("New or updated Ladies stoolball matches on the Stoolball England website", model.Metadata.Description);
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
            SetupPath("/clubs/example/rss");
            _clubDataSource.Setup(x => x.ReadClubByRoute(It.IsAny<string>())).ReturnsAsync(club);

            var model = await GetModel();

            Assert.Equal("Stoolball matches for Example club", model.Metadata.PageTitle);
            Assert.Equal(new[] { team1, team2 }, filter.TeamIds);
        }

        [Fact]
        public async Task Club_route_not_matching_club_returns_404()
        {
            SetupFilter();
            SetupPath("/clubs/example/rss");
            _clubDataSource.Setup(x => x.ReadClubByRoute(It.IsAny<string>())).Returns(Task.FromResult<Club?>(null));

            Assert.IsType<NotFoundResult>(await GetResult());
        }

        [Fact]
        public async Task Team_route_adds_team_to_filter_and_name_to_title()
        {
            var filter = SetupFilter();
            var teamId = Guid.NewGuid();
            SetupPath("/teams/example/rss");
            _teamDataSource.Setup(x => x.ReadTeamByRoute(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(new Team { TeamId = teamId, TeamName = "Example team" });

            var model = await GetModel();

            Assert.Equal("Stoolball matches for Example team", model.Metadata.PageTitle);
            Assert.Equal(new[] { teamId }, filter.TeamIds);
        }

        [Fact]
        public async Task Team_route_not_matching_team_returns_404()
        {
            SetupFilter();
            SetupPath("/teams/example/rss");
            _teamDataSource.Setup(x => x.ReadTeamByRoute(It.IsAny<string>(), It.IsAny<bool>())).Returns(Task.FromResult<Team?>(null));

            Assert.IsType<NotFoundResult>(await GetResult());
        }

        [Fact]
        public async Task Competition_route_adds_competition_to_filter_and_name_to_title()
        {
            var filter = SetupFilter();
            var competitionId = Guid.NewGuid();
            SetupPath("/competitions/example/rss");
            _competitionDataSource.Setup(x => x.ReadCompetitionByRoute(It.IsAny<string>())).ReturnsAsync(new Competition { CompetitionId = competitionId, CompetitionName = "Example league" });

            var model = await GetModel();

            Assert.Equal("Stoolball matches in the Example league", model.Metadata.PageTitle);
            Assert.Equal(new[] { competitionId }, filter.CompetitionIds);
        }

        [Fact]
        public async Task Competition_route_not_matching_competition_returns_404()
        {
            SetupFilter();
            SetupPath("/competitions/example/rss");
            _competitionDataSource.Setup(x => x.ReadCompetitionByRoute(It.IsAny<string>())).Returns(Task.FromResult<Competition?>(null));

            Assert.IsType<NotFoundResult>(await GetResult());
        }

        [Fact]
        public async Task Location_route_adds_location_to_filter_and_name_to_title()
        {
            var filter = SetupFilter();
            var locationId = Guid.NewGuid();
            var location = new MatchLocation { MatchLocationId = locationId, PrimaryAddressableObjectName = "Example ground", Town = "Exampletown" };
            SetupPath("/locations/example/rss");
            _matchLocationDataSource.Setup(x => x.ReadMatchLocationByRoute(It.IsAny<string>(), It.IsAny<bool>())).ReturnsAsync(location);

            var model = await GetModel();

            Assert.Equal("Stoolball matches at " + location.NameAndLocalityOrTown(), model.Metadata.PageTitle);
            Assert.Equal(new[] { locationId }, filter.MatchLocationIds);
        }

        [Fact]
        public async Task Location_route_not_matching_location_returns_404()
        {
            SetupFilter();
            SetupPath("/locations/example/rss");
            _matchLocationDataSource.Setup(x => x.ReadMatchLocationByRoute(It.IsAny<string>(), It.IsAny<bool>())).Returns(Task.FromResult<MatchLocation?>(null));

            Assert.IsType<NotFoundResult>(await GetResult());
        }

        [Fact]
        public async Task Other_route_does_not_read_any_entity()
        {
            SetupFilter();
            SetupPath("/matches/rss");

            await GetModel();

            _clubDataSource.VerifyNoOtherCalls();
            _teamDataSource.VerifyNoOtherCalls();
            _competitionDataSource.VerifyNoOtherCalls();
            _matchLocationDataSource.VerifyNoOtherCalls();
        }
    }
}
