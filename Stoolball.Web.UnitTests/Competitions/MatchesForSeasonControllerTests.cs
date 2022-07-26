using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Stoolball.Competitions;
using Stoolball.Matches;
using Stoolball.Security;
using Stoolball.Web.Competitions;
using Stoolball.Web.Competitions.Models;
using Stoolball.Web.Matches;
using Xunit;

namespace Stoolball.Web.UnitTests.Competitions
{
    public class MatchesForSeasonControllerTests : UmbracoBaseTest
    {
        private readonly Mock<ISeasonDataSource> _seasonDataSource = new();
        private readonly Mock<IMatchListingDataSource> _matchListingDataSource = new();

        public MatchesForSeasonControllerTests() : base()
        {
        }

        private MatchesForSeasonController CreateController()
        {
            return new MatchesForSeasonController(
                Mock.Of<ILogger<MatchesForSeasonController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                Mock.Of<IMatchFilterFactory>(),
                _seasonDataSource.Object,
                _matchListingDataSource.Object,
                Mock.Of<IAddMatchMenuViewModelFactory>(),
                Mock.Of<IAuthorizationPolicy<Competition>>()
                )
            {
                ControllerContext = ControllerContext
            };
        }

        [Fact]
        public async Task Route_not_matching_season_returns_404()
        {
            _seasonDataSource.Setup(x => x.ReadSeasonByRoute(It.IsAny<string>(), false)).Returns(Task.FromResult<Season?>(null));

            _matchListingDataSource.Setup(x => x.ReadMatchListings(It.IsAny<MatchFilter>(), MatchSortOrder.MatchDateEarliestFirst)).ReturnsAsync(new List<MatchListing>());

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<NotFoundResult>(result);
            }
        }

        [Fact]
        public async Task Route_matching_season_returns_SeasonViewModel()
        {
            _seasonDataSource.Setup(x => x.ReadSeasonByRoute(It.IsAny<string>(), false)).ReturnsAsync(new Season
            {
                SeasonId = Guid.NewGuid(),
                Competition = new Competition
                {
                    CompetitionName = "Example competition",
                    CompetitionRoute = "/competitions/example"
                },
                SeasonRoute = "/competitions/example/1234"
            });

            _matchListingDataSource.Setup(x => x.ReadMatchListings(It.IsAny<MatchFilter>(), MatchSortOrder.MatchDateEarliestFirst)).ReturnsAsync(new List<MatchListing>());

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<SeasonViewModel>(((ViewResult)result).Model);
            }
        }
    }
}
