using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Stoolball.Clubs;
using Stoolball.Data.Abstractions;
using Stoolball.Dates;
using Stoolball.Matches;
using Stoolball.Security;
using Stoolball.Teams;
using Stoolball.Web.Clubs;
using Xunit;

namespace Stoolball.Web.UnitTests.Clubs
{
    public class MatchesForClubControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IClubDataSource> _clubDataSource = new();
        private readonly Mock<IMatchListingDataSource> _matchListingDataSource = new();
        private readonly Mock<IMatchFilterFactory> _matchFilterFactory = new();
        private readonly Mock<IMatchFilterQueryStringParser> _matchFilterQueryStringParser = new();
        private readonly Mock<IMatchFilterHumanizer> _matchFilterHumanizer = new();
        private readonly Mock<IAuthorizationPolicy<Club>> _authorizationPolicy = new();
        private readonly MatchFilter _defaultFilter = new();
        private readonly MatchFilter _appliedFilter = new();
        private readonly Club _club = new() { ClubName = "Example club" };

        public MatchesForClubControllerTests()
        {
            _matchFilterFactory.Setup(x => x.MatchesForTeams(It.IsAny<List<Guid>>())).Returns((_defaultFilter, MatchSortOrder.MatchDateEarliestFirst));
            _matchFilterQueryStringParser.Setup(x => x.ParseQueryString(_defaultFilter, It.IsAny<string>())).Returns(_appliedFilter);
            _matchFilterHumanizer.Setup(x => x.MatchesAndTournaments(_appliedFilter)).Returns("Matches");
            _matchFilterHumanizer.Setup(x => x.MatchingFilter(_appliedFilter)).Returns(string.Empty);
            _authorizationPolicy.Setup(x => x.IsAuthorized(_club)).ReturnsAsync(new Dictionary<AuthorizedAction, bool> { { AuthorizedAction.EditClub, true } });
            _clubDataSource.Setup(x => x.ReadClubByRoute(It.IsAny<string>())).ReturnsAsync(_club);
        }

        private MatchesForClubController CreateController()
        {
            return new MatchesForClubController(
                Mock.Of<ILogger<MatchesForClubController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _clubDataSource.Object,
                _matchListingDataSource.Object,
                Mock.Of<IDateTimeFormatter>(),
                _matchFilterFactory.Object,
                _authorizationPolicy.Object,
                _matchFilterQueryStringParser.Object,
                _matchFilterHumanizer.Object)
            {
                ControllerContext = ControllerContext
            };
        }

        private async Task<ClubViewModel> Run()
        {
            using (var controller = CreateController())
            {
                var result = await controller.Index();

                return Assert.IsType<ClubViewModel>(((ViewResult)result).Model);
            }
        }

        [Fact]
        public async Task Route_not_matching_club_returns_404()
        {
            _clubDataSource.Setup(x => x.ReadClubByRoute(It.IsAny<string>())).Returns(Task.FromResult<Club?>(null));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<NotFoundResult>(result);
                _matchListingDataSource.Verify(x => x.ReadMatchListings(It.IsAny<MatchFilter>(), It.IsAny<MatchSortOrder>()), Times.Never);
            }
        }

        [Fact]
        public async Task Route_matching_club_returns_ClubViewModel_containing_club()
        {
            var model = await Run();

            Assert.Equal(_club, model.Club);
        }

        [Fact]
        public async Task Route_matching_club_builds_filter_from_team_ids()
        {
            var team1 = new Team { TeamId = Guid.NewGuid() };
            var team2 = new Team { TeamId = Guid.NewGuid() };
            _club.Teams.Add(team1);
            _club.Teams.Add(team2);

            await Run();

            _matchFilterFactory.Verify(x => x.MatchesForTeams(It.Is<List<Guid>>(ids => ids.Count == 2 && ids.Contains(team1.TeamId!.Value) && ids.Contains(team2.TeamId!.Value))), Times.Once);
        }

        [Fact]
        public async Task Route_matching_club_sets_default_and_applied_filters()
        {
            var model = await Run();

            Assert.Same(_defaultFilter, model.DefaultMatchFilter);
            Assert.Same(_appliedFilter, model.AppliedMatchFilter);
        }

        [Fact]
        public async Task Route_matching_club_parses_query_string_against_default_filter()
        {
            Request.SetupGet(x => x.QueryString).Returns(new Microsoft.AspNetCore.Http.QueryString("?from=2020-01-01"));

            await Run();

            _matchFilterQueryStringParser.Verify(x => x.ParseQueryString(_defaultFilter, "?from=2020-01-01"), Times.Once);
        }

        [Fact]
        public async Task Club_without_teams_does_not_read_matches()
        {
            var model = await Run();

            _matchListingDataSource.Verify(x => x.ReadMatchListings(It.IsAny<MatchFilter>(), It.IsAny<MatchSortOrder>()), Times.Never);
            Assert.Empty(model.Matches.Matches);
        }

        [Fact]
        public async Task Club_with_teams_reads_matches_using_applied_filter_and_sort_order()
        {
            _club.Teams.Add(new Team { TeamId = Guid.NewGuid() });
            var matches = new List<MatchListing> { new MatchListing { MatchName = "Match 1" } };
            _matchListingDataSource.Setup(x => x.ReadMatchListings(_appliedFilter, MatchSortOrder.MatchDateEarliestFirst)).ReturnsAsync(matches);

            var model = await Run();

            _matchListingDataSource.Verify(x => x.ReadMatchListings(_appliedFilter, MatchSortOrder.MatchDateEarliestFirst), Times.Once);
            Assert.Equal(matches, model.Matches.Matches);
        }

        [Fact]
        public async Task Route_matching_club_sets_authorization()
        {
            var model = await Run();

            Assert.True(model.Authorization.CurrentMemberIsAuthorized[AuthorizedAction.EditClub]);
        }

        [Fact]
        public async Task Without_user_filter_page_title_has_no_filter_and_description_is_empty()
        {
            var model = await Run();

            Assert.Equal("Matches for Example club", model.Metadata.PageTitle);
            Assert.Null(model.FilterDescription);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("  ")]
        public async Task Blank_user_filter_does_not_set_filter_description(string? userFilter)
        {
            _matchFilterHumanizer.Setup(x => x.MatchingFilter(_appliedFilter)).Returns(userFilter!);

            var model = await Run();

            Assert.Null(model.FilterDescription);
        }

        [Fact]
        public async Task User_filter_is_added_to_page_title_and_filter_description()
        {
            _matchFilterHumanizer.Setup(x => x.MatchingFilter(_appliedFilter)).Returns(" in 2020");

            var model = await Run();

            Assert.Equal("Matches in 2020", model.FilterDescription);
            Assert.Equal("Matches for Example club in 2020", model.Metadata.PageTitle);
        }

        [Fact]
        public async Task Route_matching_club_adds_teams_breadcrumb()
        {
            var model = await Run();

            var breadcrumb = model.Breadcrumbs[^1];
            Assert.Equal(Constants.Pages.Teams, breadcrumb.Name);
            Assert.Equal(new Uri(Constants.Pages.TeamsUrl, UriKind.Relative), breadcrumb.Url);
        }
    }
}
