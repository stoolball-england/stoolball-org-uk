using Moq;
using Stoolball.Competitions;
using Stoolball.Matches;
using Stoolball.Teams;
using Stoolball.Umbraco.Data.Competitions;
using Stoolball.Umbraco.Data.Teams;
using Stoolball.Web.Matches;
using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Umbraco.Core.Cache;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Web;
using Umbraco.Web.Models;
using Xunit;

namespace Stoolball.Web.Tests.Matches
{
    public class CreateKnockoutMatchControllerTests : UmbracoBaseTest
    {
        private class TestController : CreateKnockoutMatchController
        {
            public TestController(ITeamDataSource teamDataSource, ISeasonDataSource seasonDataSource, ICreateMatchSeasonSelector createMatchSeasonSelector, IEditMatchHelper editMatchHelper, Uri requestUrl)
           : base(
                Mock.Of<IGlobalSettings>(),
                Mock.Of<IUmbracoContextAccessor>(),
                null,
                AppCaches.NoCache,
                Mock.Of<IProfilingLogger>(),
                null,
                teamDataSource,
                seasonDataSource,
                createMatchSeasonSelector,
                editMatchHelper)
            {
                var request = new Mock<HttpRequestBase>();
                request.SetupGet(x => x.RawUrl).Returns(requestUrl.AbsolutePath);

                var context = new Mock<HttpContextBase>();
                context.SetupGet(x => x.Request).Returns(request.Object);

                var controllerContext = new Mock<ControllerContext>();
                controllerContext.Setup(p => p.HttpContext).Returns(context.Object);
                controllerContext.Setup(p => p.HttpContext.User).Returns(new GenericPrincipal(new GenericIdentity("test"), null));
                ControllerContext = controllerContext.Object;
            }

            protected override ActionResult CurrentTemplate<T>(T model)
            {
                return View("CreateKnockoutMatch", model);
            }
        }

        [Fact]
        public async Task Route_not_matching_team_returns_404()
        {
            var teamDataSource = new Mock<ITeamDataSource>();
            teamDataSource.Setup(x => x.ReadTeamByRoute(It.IsAny<string>(), true)).Returns(Task.FromResult<Team>(null));

            var seasonDataSource = new Mock<ISeasonDataSource>();

            var seasonSelector = new Mock<ICreateMatchSeasonSelector>();

            var helper = new Mock<IEditMatchHelper>();

            using (var controller = new TestController(teamDataSource.Object, seasonDataSource.Object, seasonSelector.Object, helper.Object, new Uri("https://example.org/teams/example/")))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<HttpNotFoundResult>(result);
            }
        }

        [Fact]
        public async Task Route_not_matching_season_returns_404()
        {
            var teamDataSource = new Mock<ITeamDataSource>();

            var seasonDataSource = new Mock<ISeasonDataSource>();
            seasonDataSource.Setup(x => x.ReadSeasonByRoute(It.IsAny<string>(), true)).Returns(Task.FromResult<Season>(null));

            var seasonSelector = new Mock<ICreateMatchSeasonSelector>();

            var helper = new Mock<IEditMatchHelper>();

            using (var controller = new TestController(teamDataSource.Object, seasonDataSource.Object, seasonSelector.Object, helper.Object, new Uri("https://example.org/competitions/example/2020/")))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<HttpNotFoundResult>(result);
            }
        }

        [Fact]
        public async Task Route_matching_team_without_a_knockout_competition_returns_404()
        {
            var teamDataSource = new Mock<ITeamDataSource>();
            teamDataSource.Setup(x => x.ReadTeamByRoute(It.IsAny<string>(), true)).Returns(Task.FromResult(new Team()));

            var seasonDataSource = new Mock<ISeasonDataSource>();

            var seasonSelector = new Mock<ICreateMatchSeasonSelector>();
            seasonSelector.Setup(x => x.SelectPossibleSeasons(It.IsAny<IEnumerable<TeamInSeason>>(), MatchType.KnockoutMatch)).Returns(new List<Season>());

            var helper = new Mock<IEditMatchHelper>();
            helper.Setup(x => x.PossibleSeasonsAsListItems(It.IsAny<IEnumerable<Season>>())).Returns(new List<SelectListItem>());

            using (var controller = new TestController(teamDataSource.Object, seasonDataSource.Object, seasonSelector.Object, helper.Object, new Uri("https://example.org/teams/example/")))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<HttpNotFoundResult>(result);
            }
        }


        [Fact]
        public async Task Route_matching_non_knockout_season_returns_404()
        {
            var teamDataSource = new Mock<ITeamDataSource>();

            var seasonDataSource = new Mock<ISeasonDataSource>();
            seasonDataSource.Setup(x => x.ReadSeasonByRoute(It.IsAny<string>(), true)).Returns(Task.FromResult(new Season()));

            var seasonSelector = new Mock<ICreateMatchSeasonSelector>();

            var helper = new Mock<IEditMatchHelper>();

            using (var controller = new TestController(teamDataSource.Object, seasonDataSource.Object, seasonSelector.Object, helper.Object, new Uri("https://example.org/competitions/")))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<HttpNotFoundResult>(result);
            }
        }


        [Fact]
        public async Task Route_matching_team_playing_knockout_matches_returns_CreateKnockoutMatchViewModel()
        {
            var teamDataSource = new Mock<ITeamDataSource>();
            teamDataSource.Setup(x => x.ReadTeamByRoute(It.IsAny<string>(), true)).Returns(Task.FromResult(new Team()));

            var seasonDataSource = new Mock<ISeasonDataSource>();

            var seasonSelector = new Mock<ICreateMatchSeasonSelector>();
            seasonSelector.Setup(x => x.SelectPossibleSeasons(It.IsAny<IEnumerable<TeamInSeason>>(), MatchType.KnockoutMatch)).Returns(new List<Season> { new Season() });

            var helper = new Mock<IEditMatchHelper>();

            using (var controller = new TestController(teamDataSource.Object, seasonDataSource.Object, seasonSelector.Object, helper.Object, new Uri("https://example.org/teams/example/")))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<EditKnockoutMatchViewModel>(((ViewResult)result).Model);
            }
        }


        [Fact]
        public async Task Route_matching_knockout_season_returns_CreateKnockoutMatchViewModel()
        {
            var teamDataSource = new Mock<ITeamDataSource>();

            var seasonDataSource = new Mock<ISeasonDataSource>();
            seasonDataSource.Setup(x => x.ReadSeasonByRoute(It.IsAny<string>(), true)).Returns(Task.FromResult(new Season
            {
                MatchTypes = new List<MatchType> { MatchType.KnockoutMatch }
            }));

            var seasonSelector = new Mock<ICreateMatchSeasonSelector>();

            var helper = new Mock<IEditMatchHelper>();

            using (var controller = new TestController(teamDataSource.Object, seasonDataSource.Object, seasonSelector.Object, helper.Object, new Uri("https://example.org/competitions/example/2020/")))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<EditKnockoutMatchViewModel>(((ViewResult)result).Model);
            }
        }
    }
}
