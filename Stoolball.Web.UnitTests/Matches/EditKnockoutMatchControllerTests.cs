using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using Moq;
using Stoolball.Competitions;
using Stoolball.Dates;
using Stoolball.Matches;
using Stoolball.Security;
using Stoolball.Web.Matches;
using Stoolball.Web.Matches.Models;
using Xunit;

namespace Stoolball.Web.UnitTests.Matches
{
    public class EditKnockoutMatchControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IMatchDataSource> _matchDataSource = new();
        private readonly Mock<ISeasonDataSource> _seasonDataSource = new();
        private readonly Mock<IEditMatchHelper> _editMatchHelper = new();

        private EditKnockoutMatchController CreateController()
        {
            return new EditKnockoutMatchController(
                Mock.Of<ILogger<EditKnockoutMatchController>>(),
                CompositeViewEngine.Object,
                UmbracoContextAccessor.Object,
                _matchDataSource.Object,
                _seasonDataSource.Object,
                Mock.Of<IAuthorizationPolicy<Stoolball.Matches.Match>>(),
                Mock.Of<IDateTimeFormatter>(),
                _editMatchHelper.Object)
            {
                ControllerContext = ControllerContext
            };
        }

        [Fact]
        public async Task Route_not_matching_match_returns_404()
        {
            Request.SetupGet(x => x.Path).Returns(new PathString("/not-a-match"));
            _matchDataSource.Setup(x => x.ReadMatchByRoute(It.IsAny<string>())).Returns(Task.FromResult<Stoolball.Matches.Match?>(null));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<NotFoundResult>(result);
            }
        }


        [Fact]
        public async Task Route_matching_match_in_the_past_returns_404()
        {
            Request.SetupGet(x => x.Path).Returns(new PathString("/matches/example-match"));
            _matchDataSource.Setup(x => x.ReadMatchByRoute(It.IsAny<string>())).ReturnsAsync(new Stoolball.Matches.Match { StartTime = DateTime.UtcNow.AddHours(-1), Season = new Season() });
            _seasonDataSource.Setup(x => x.ReadSeasonByRoute(It.IsAny<string>(), true)).Returns(Task.FromResult(new Season()));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<NotFoundResult>(result);
            }
        }

        [Fact]
        public async Task Route_matching_match_in_the_future_returns_EditKnockoutMatchViewModel()
        {
            Request.SetupGet(x => x.Path).Returns(new PathString("/matches/example-match"));
            var season = new Season { Competition = new Competition { CompetitionName = "Example competition", CompetitionRoute = "/competitions/example" }, SeasonRoute = "/competitions/example/2021" };
            _matchDataSource.Setup(x => x.ReadMatchByRoute(It.IsAny<string>())).ReturnsAsync(new Stoolball.Matches.Match
            {
                StartTime = DateTime.UtcNow.AddHours(1),
                Season = season,
                MatchRoute = "/matches/example"
            });

            _seasonDataSource.Setup(x => x.ReadSeasonByRoute(It.IsAny<string>(), true)).Returns(Task.FromResult(season));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.IsType<EditKnockoutMatchViewModel>(((ViewResult)result).Model);
            }
        }

        [Fact]
        public async Task MatchU002ESeason_gets_SeasonId_from_Route()
        {
            Request.SetupGet(x => x.Path).Returns(new PathString("/matches/example-match"));
            _matchDataSource.Setup(x => x.ReadMatchByRoute(It.IsAny<string>())).ReturnsAsync(new Stoolball.Matches.Match
            {
                StartTime = DateTime.UtcNow.AddHours(1),
                Season = new Season { Competition = new Competition { CompetitionName = "Example competition", CompetitionRoute = "/competitions/example" }, SeasonRoute = "/competitions/example/2021" },
                MatchRoute = "/matches/example"
            });

            var season = new Season { SeasonId = Guid.NewGuid(), Competition = new Competition { CompetitionName = "Example competition", CompetitionRoute = "/competitions/example" }, SeasonRoute = "/competitions/example/2021" };
            _seasonDataSource.Setup(x => x.ReadSeasonByRoute(It.IsAny<string>(), true)).Returns(Task.FromResult(season));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.Equal(season.SeasonId, ((IEditMatchViewModel)((ViewResult)result).Model).Match?.Season.SeasonId);
            }
        }

        [Fact]
        public async Task ModelU002ESeason_gets_SeasonId_from_Route()
        {
            Request.SetupGet(x => x.Path).Returns(new PathString("/matches/example-match"));
            _matchDataSource.Setup(x => x.ReadMatchByRoute(It.IsAny<string>())).ReturnsAsync(new Stoolball.Matches.Match
            {
                StartTime = DateTime.UtcNow.AddHours(1),
                Season = new Season { Competition = new Competition { CompetitionName = "Example competition", CompetitionRoute = "/competitions/example" }, SeasonRoute = "/competitions/example/2021" },
                MatchRoute = "/matches/example"
            });

            var season = new Season { SeasonId = Guid.NewGuid(), Competition = new Competition { CompetitionName = "Example competition", CompetitionRoute = "/competitions/example" }, SeasonRoute = "/competitions/example/2021" };
            _seasonDataSource.Setup(x => x.ReadSeasonByRoute(It.IsAny<string>(), true)).Returns(Task.FromResult(season));

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.Equal(season.SeasonId, ((IEditMatchViewModel)((ViewResult)result).Model).Season?.SeasonId);
            }
        }

        [Fact]
        public async Task ModelU002EPossibleSeasons_gets_SeasonId_from_Route()
        {
            Request.SetupGet(x => x.Path).Returns(new PathString("/matches/example-match"));
            _matchDataSource.Setup(x => x.ReadMatchByRoute(It.IsAny<string>())).ReturnsAsync(new Stoolball.Matches.Match
            {
                StartTime = DateTime.UtcNow.AddHours(1),
                Season = new Season { Competition = new Competition { CompetitionName = "Example competition", CompetitionRoute = "/competitions/example" }, SeasonRoute = "/competitions/example/2021" },
                MatchRoute = "/matches/example"
            });

            var season = new Season { SeasonId = Guid.NewGuid(), Competition = new Competition { CompetitionName = "Example competition", CompetitionRoute = "/competitions/example" }, SeasonRoute = "/competitions/example/2021" };
            _seasonDataSource.Setup(x => x.ReadSeasonByRoute(It.IsAny<string>(), true)).Returns(Task.FromResult(season));

            _editMatchHelper.Setup(x => x.PossibleSeasonsAsListItems(It.IsAny<IEnumerable<Season>>())).Returns((IEnumerable<Season> x) => new List<SelectListItem> { new SelectListItem { Value = season.SeasonId.ToString() } });

            using (var controller = CreateController())
            {
                var result = await controller.Index();

                Assert.Equal(season.SeasonId.ToString(), ((IEditMatchViewModel)((ViewResult)result).Model).PossibleSeasons.First().Value);
            }
        }
    }
}
