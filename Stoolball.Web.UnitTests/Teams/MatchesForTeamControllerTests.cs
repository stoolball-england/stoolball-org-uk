using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Stoolball.Competitions;
using Stoolball.Dates;
using Stoolball.Matches;
using Stoolball.Security;
using Stoolball.Teams;
using Stoolball.Web.Matches;
using Stoolball.Web.Teams;
using Stoolball.Web.Teams.Models;
using Xunit;

namespace Stoolball.Web.UnitTests.Teams
{
    public class MatchesForTeamControllerTests : UmbracoBaseTest
    {
        private readonly Mock<ITeamDataSource> _teamDataSource = new();
        private readonly Mock<IMatchListingDataSource> _matchListingDataSource = new();
        private readonly Mock<ICreateMatchSeasonSelector> _createMatchSeasonSelector = new();
        private readonly Mock<IMatchFilterQueryStringParser> _matchFilterQueryStringParser = new();

        public MatchesForTeamControllerTests() : base()
        {
        }

        private MatchesForTeamController CreateController()
        {
            return new MatchesForTeamController(
                Mock.Of<ILogger<MatchesForTeamController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _teamDataSource.Object,
                Mock.Of<IMatchFilterFactory>(),
                _matchListingDataSource.Object,
                Mock.Of<IDateTimeFormatter>(),
                _createMatchSeasonSelector.Object,
                Mock.Of<IAuthorizationPolicy<Team>>(),
                _matchFilterQueryStringParser.Object,
                Mock.Of<IMatchFilterHumanizer>(),
                Mock.Of<IAddMatchMenuViewModelFactory>())
            {
                ControllerContext = ControllerContext
            };
        }

        [Fact]
        public async Task Route_not_matching_team_returns_404()
        {
            _teamDataSource.Setup(x => x.ReadTeamByRoute(It.IsAny<string>(), false)).Returns(Task.FromResult<Team?>(null));

            var filter = new MatchFilter();
            _matchFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<MatchFilter>(), It.IsAny<string>())).Returns(filter);

            _matchListingDataSource.Setup(x => x.ReadMatchListings(filter, MatchSortOrder.MatchDateEarliestFirst)).ReturnsAsync(new List<MatchListing>());

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<NotFoundResult>(result);
            }
        }

        [Fact]
        public async Task Route_matching_team_returns_TeamViewModel()
        {
            _teamDataSource.Setup(x => x.ReadTeamByRoute(It.IsAny<string>(), true)).ReturnsAsync(new Team { TeamId = Guid.NewGuid() });

            var filter = new MatchFilter();
            _matchFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<MatchFilter>(), It.IsAny<string>())).Returns(filter);

            _matchListingDataSource.Setup(x => x.ReadMatchListings(filter, MatchSortOrder.MatchDateEarliestFirst)).ReturnsAsync(new List<MatchListing>());

            _createMatchSeasonSelector.Setup(x => x.SelectPossibleSeasons(It.IsAny<IEnumerable<TeamInSeason>>(), It.IsAny<MatchType>())).Returns(new List<Season>());

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<TeamViewModel>(((ViewResult)result).Model);
            }
        }
    }
}
