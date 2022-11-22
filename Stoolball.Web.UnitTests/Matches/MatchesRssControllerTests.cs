using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Stoolball.Data.Abstractions;
using Stoolball.Matches;
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
    }
}
