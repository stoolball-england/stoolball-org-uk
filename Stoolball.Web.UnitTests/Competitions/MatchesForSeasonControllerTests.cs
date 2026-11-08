using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Stoolball.Competitions;
using Stoolball.Data.Abstractions;
using Stoolball.Matches;
using Stoolball.Security;
using Stoolball.Web.Competitions;
using Stoolball.Web.Competitions.Models;
using Stoolball.Web.Matches;
using Stoolball.Web.Matches.Models;
using Xunit;

namespace Stoolball.Web.UnitTests.Competitions
{
    public class MatchesForSeasonControllerTests : UmbracoBaseTest
    {
        private readonly Mock<ISeasonDataSource> _seasonDataSource = new();
        private readonly Mock<IMatchListingDataSource> _matchListingDataSource = new();
        private readonly Mock<IMatchFilterFactory> _matchFilterFactory = new();
        private readonly Mock<IAddMatchMenuViewModelFactory> _addMatchMenuViewModelFactory = new();
        private readonly Mock<IAuthorizationPolicy<Competition>> _authorizationPolicy = new();

        private MatchesForSeasonController CreateController()
        {
            return new MatchesForSeasonController(
                Mock.Of<ILogger<MatchesForSeasonController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _matchFilterFactory.Object,
                _seasonDataSource.Object,
                _matchListingDataSource.Object,
                _addMatchMenuViewModelFactory.Object,
                _authorizationPolicy.Object)
            {
                ControllerContext = ControllerContext
            };
        }

        private Season SetupSeason()
        {
            var season = new Season
            {
                SeasonId = Guid.NewGuid(),
                FromYear = 2026,
                UntilYear = 2026,
                Competition = new Competition { CompetitionName = "Example competition", CompetitionRoute = "/competitions/example" },
                SeasonRoute = "/competitions/example/1234"
            };
            _seasonDataSource.Setup(x => x.ReadSeasonByRoute(It.IsAny<string>(), false)).ReturnsAsync(season);
            _matchFilterFactory.Setup(x => x.MatchesForSeason(season.SeasonId.Value)).Returns((new MatchFilter(), MatchSortOrder.MatchDateEarliestFirst));
            _matchListingDataSource.Setup(x => x.ReadMatchListings(It.IsAny<MatchFilter>(), MatchSortOrder.MatchDateEarliestFirst)).ReturnsAsync(new List<MatchListing>());
            _addMatchMenuViewModelFactory.Setup(x => x.CreateModel(It.IsAny<string?>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>())).Returns(new AddMatchMenuViewModel());
            return season;
        }

        private async Task<SeasonViewModel> ReadModel()
        {
            using var controller = CreateController();

            var result = await controller.Index();

            return Assert.IsType<SeasonViewModel>(((ViewResult)result).Model);
        }

        [Fact]
        public async Task Route_not_matching_season_returns_404()
        {
            _seasonDataSource.Setup(x => x.ReadSeasonByRoute(It.IsAny<string>(), false)).Returns(Task.FromResult<Season?>(null));

            using var controller = CreateController();

            var result = await controller.Index();

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Route_matching_season_returns_SeasonViewModel_with_season()
        {
            var season = SetupSeason();

            var model = await ReadModel();

            Assert.Equal(season, model.Season);
        }

        [Fact]
        public async Task Matches_are_read_using_filter_for_the_season()
        {
            var season = SetupSeason();
            var filter = new MatchFilter();
            var matches = new List<MatchListing> { new() { MatchName = "Example match" } };
            _matchFilterFactory.Setup(x => x.MatchesForSeason(season.SeasonId!.Value)).Returns((filter, MatchSortOrder.LatestUpdateFirst));
            _matchListingDataSource.Setup(x => x.ReadMatchListings(filter, MatchSortOrder.LatestUpdateFirst)).ReturnsAsync(matches);

            var model = await ReadModel();

            Assert.Equal(matches, model.Matches.Matches);
        }

        [Theory]
        [InlineData(MatchType.LeagueMatch)]
        [InlineData(MatchType.KnockoutMatch)]
        public async Task Friendly_matches_are_labelled_for_league_and_knockout_seasons(MatchType matchType)
        {
            var season = SetupSeason();
            season.MatchTypes.Add(matchType);

            var model = await ReadModel();

            Assert.Contains(MatchType.FriendlyMatch, model.Matches.MatchTypesToLabel);
        }

        [Fact]
        public async Task Friendly_matches_are_not_labelled_when_season_has_no_league_or_knockout_matches()
        {
            var season = SetupSeason();
            season.MatchTypes.Add(MatchType.FriendlyMatch);

            var model = await ReadModel();

            Assert.Empty(model.Matches.MatchTypesToLabel);
        }

        [Fact]
        public async Task Add_match_menu_is_created_from_season_match_types()
        {
            var season = SetupSeason();
            season.MatchTypes.AddRange([MatchType.TrainingSession, MatchType.LeagueMatch]);
            season.EnableTournaments = true;
            var menu = new AddMatchMenuViewModel();
            _addMatchMenuViewModelFactory.Setup(x => x.CreateModel(season.SeasonRoute, true, true, false, false, true, true)).Returns(menu);

            var model = await ReadModel();

            Assert.Equal(menu, model.AddMatchMenu);
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
        public async Task Model_has_page_title_and_breadcrumbs()
        {
            var season = SetupSeason();

            var model = await ReadModel();

            Assert.Equal($"Matches and tournaments in {season.SeasonFullNameAndPlayerType()}", model.Metadata.PageTitle);
            Assert.Equal([Constants.Pages.Competitions, "Example competition"], model.Breadcrumbs.Select(x => x.Name).TakeLast(2));
        }
    }
}
