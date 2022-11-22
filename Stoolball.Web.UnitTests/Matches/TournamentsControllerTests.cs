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
    public class TournamentsControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IMatchListingDataSource> _matchesDataSource = new();
        private readonly Mock<IMatchFilterQueryStringParser> _matchFilterQueryStringParser = new();
        private readonly Mock<IMatchFilterHumanizer> _matchFilterHumanizer = new();

        private TournamentsController CreateController()
        {
            return new TournamentsController(
                Mock.Of<ILogger<TournamentsController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _matchesDataSource.Object,
                _matchFilterQueryStringParser.Object,
                _matchFilterHumanizer.Object)
            {
                ControllerContext = ControllerContext
            };
        }

        [Fact]
        public async Task Returns_MatchListingViewModel()
        {
            _matchFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<MatchFilter>(), It.IsAny<string>())).Returns(new MatchFilter());

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<MatchListingViewModel>(((ViewResult)result).Model);
            }
        }

        [Fact]
        public async Task Assigns_MatchFilter_to_view_model()
        {
            var filter = new MatchFilter();
            _matchFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<MatchFilter>(), It.IsAny<string>())).Returns(filter);

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.Equal(filter, ((MatchListingViewModel)((ViewResult)result).Model).AppliedMatchFilter);
            }
        }


        [Fact]
        public async Task Page_title_is_set_to_humanized_filter()
        {
            var filter = new MatchFilter();
            _matchFilterQueryStringParser.Setup(x => x.ParseQueryString(It.IsAny<MatchFilter>(), It.IsAny<string>())).Returns(filter);
            _matchFilterHumanizer.Setup(x => x.MatchesAndTournaments(filter)).Returns("tournaments");
            _matchFilterHumanizer.Setup(x => x.MatchingFilter(filter)).Returns(" matching filter");

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.Equal("tournaments matching filter", ((MatchListingViewModel)((ViewResult)result).Model).Metadata.PageTitle);
            }
        }
    }
}
