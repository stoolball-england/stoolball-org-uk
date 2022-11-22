using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Stoolball.Competitions;
using Stoolball.Data.Abstractions;
using Stoolball.Email;
using Stoolball.Matches;
using Stoolball.Security;
using Stoolball.Web.Competitions;
using Stoolball.Web.Competitions.Models;
using Xunit;

namespace Stoolball.Web.UnitTests.Competitions
{
    public class SeasonResultsTableControllerTests : UmbracoBaseTest
    {
        private readonly Mock<ISeasonDataSource> _seasonDataSource = new();
        private readonly Mock<IMatchListingDataSource> _matchListingDataSource = new();

        private SeasonResultsTableController CreateController()
        {
            return new SeasonResultsTableController(
                Mock.Of<ILogger<SeasonResultsTableController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _seasonDataSource.Object,
                _matchListingDataSource.Object,
                Mock.Of<IAuthorizationPolicy<Competition>>(),
                Mock.Of<IEmailProtector>()
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
            _seasonDataSource.Setup(x => x.ReadSeasonByRoute(It.IsAny<string>(), true)).ReturnsAsync(new Season
            {
                SeasonId = Guid.NewGuid(),
                Competition = new Competition
                {
                    CompetitionName = "Example",
                    CompetitionRoute = "/competitions/example"
                },
                Results = "Example",
                SeasonRoute = "/competitions/example/1234"
            });

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<SeasonViewModel>(((ViewResult)result).Model);
            }
        }

        [Fact]
        public async Task Route_matching_season_with_league_matches_returns_SeasonViewModel()
        {
            _seasonDataSource.Setup(x => x.ReadSeasonByRoute(It.IsAny<string>(), true)).ReturnsAsync(new Season
            {
                SeasonId = Guid.NewGuid(),
                Competition = new Competition
                {
                    CompetitionName = "Example",
                    CompetitionRoute = "/competitions/example"
                },
                MatchTypes = new List<MatchType>() { MatchType.LeagueMatch },
                SeasonRoute = "/competitions/example/1234"
            });

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<SeasonViewModel>(((ViewResult)result).Model);
            }
        }

    }
}
