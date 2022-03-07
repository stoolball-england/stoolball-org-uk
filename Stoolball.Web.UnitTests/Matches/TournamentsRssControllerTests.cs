using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Stoolball.Matches;
using Stoolball.Web.Matches;
using Stoolball.Web.Matches.Models;
using Xunit;

namespace Stoolball.Web.UnitTests.Matches
{
    public class TournamentsRssControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IMatchListingDataSource> _matchDataSource = new();
        private readonly Mock<IMatchFilterQueryStringParser> _matchFilterQueryStringParser = new();
        private readonly Mock<IMatchFilterHumanizer> _matchFilterHumanizer = new();

        public TournamentsRssControllerTests()
        {
            Setup();
        }

        private TournamentsRssController CreateController()
        {
            return new TournamentsRssController(
                Mock.Of<ILogger<TournamentsRssController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _matchDataSource.Object,
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
            _matchFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<MatchFilter>(), It.IsAny<string>())).Returns(filter);
            _matchDataSource.Setup(x => x.ReadMatchListings(filter, MatchSortOrder.LatestUpdateFirst)).ReturnsAsync(new List<MatchListing>());

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<MatchListingViewModel>(((ViewResult)result).Model);
            }
        }
    }
}
