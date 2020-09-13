using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Moq;
using Stoolball.Competitions;
using Stoolball.Dates;
using Stoolball.Matches;
using Stoolball.Security;
using Stoolball.Umbraco.Data.Matches;
using Stoolball.Web.Matches;
using Umbraco.Core.Cache;
using Umbraco.Core.Configuration;
using Umbraco.Core.Logging;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Web;
using Umbraco.Web.Models;
using Xunit;

namespace Stoolball.Web.Tests.Matches
{
    public class EditBowlingScorecardControllerTests : UmbracoBaseTest
    {
        public EditBowlingScorecardControllerTests()
        {
            Setup();
        }

        private class TestController : EditBowlingScorecardController
        {
            public TestController(IMatchDataSource matchDataSource, Uri requestUrl, IMatchInningsUrlParser urlParser, UmbracoHelper umbracoHelper)
           : base(
                Mock.Of<IGlobalSettings>(),
                Mock.Of<IUmbracoContextAccessor>(),
                null,
                AppCaches.NoCache,
                Mock.Of<IProfilingLogger>(),
                umbracoHelper,
                matchDataSource,
                Mock.Of<IAuthorizationPolicy<Stoolball.Matches.Match>>(),
                Mock.Of<IDateTimeFormatter>(),
                urlParser)
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
                return View("EditBowlingScorecard", model);
            }
        }

        [Fact]
        public async Task Route_not_matching_match_returns_404()
        {
            var matchDataSource = new Mock<IMatchDataSource>();
            matchDataSource.Setup(x => x.ReadMatchByRoute(It.IsAny<string>())).Returns(Task.FromResult<Stoolball.Matches.Match>(null));

            var urlParser = new Mock<IMatchInningsUrlParser>();
            urlParser.Setup(x => x.ParseInningsOrderInMatchFromUrl(It.IsAny<Uri>())).Returns(1);

            using (var controller = new TestController(matchDataSource.Object, new Uri("https://example.org/not-a-match"), urlParser.Object, UmbracoHelper))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<HttpNotFoundResult>(result);
            }
        }


        [Fact]
        public async Task Route_matching_match_in_the_future_returns_404()
        {
            var matchDataSource = new Mock<IMatchDataSource>();
            matchDataSource.Setup(x => x.ReadMatchByRoute(It.IsAny<string>())).ReturnsAsync(new Stoolball.Matches.Match { StartTime = DateTime.UtcNow.AddHours(1), Season = new Season() });

            var urlParser = new Mock<IMatchInningsUrlParser>();
            urlParser.Setup(x => x.ParseInningsOrderInMatchFromUrl(It.IsAny<Uri>())).Returns(1);

            using (var controller = new TestController(matchDataSource.Object, new Uri("https://example.org/matches/example-match"), urlParser.Object, UmbracoHelper))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<HttpNotFoundResult>(result);
            }
        }

        [Theory]
        [InlineData(MatchResultType.HomeWinByForfeit)]
        [InlineData(MatchResultType.AwayWinByForfeit)]
        [InlineData(MatchResultType.Postponed)]
        [InlineData(MatchResultType.Cancelled)]
        public async Task Route_matching_match_with_not_played_result_returns_404(MatchResultType matchResultType)
        {
            var matchDataSource = new Mock<IMatchDataSource>();
            matchDataSource.Setup(x => x.ReadMatchByRoute(It.IsAny<string>())).ReturnsAsync(new Stoolball.Matches.Match
            {
                StartTime = DateTime.UtcNow.AddHours(1),
                MatchResultType = matchResultType,
                Season = new Season()
            });

            var urlParser = new Mock<IMatchInningsUrlParser>();
            urlParser.Setup(x => x.ParseInningsOrderInMatchFromUrl(It.IsAny<Uri>())).Returns(1);

            using (var controller = new TestController(matchDataSource.Object, new Uri("https://example.org/matches/example-match"), urlParser.Object, UmbracoHelper))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<HttpNotFoundResult>(result);
            }
        }

        [Fact]
        public async Task Route_not_matching_innings_returns_404()
        {
            var matchDataSource = new Mock<IMatchDataSource>();
            matchDataSource.Setup(x => x.ReadMatchByRoute(It.IsAny<string>())).ReturnsAsync(new Stoolball.Matches.Match
            {
                StartTime = DateTime.UtcNow.AddHours(1),
                MatchResultType = MatchResultType.HomeWin,
                Season = new Season()
            });

            var urlParser = new Mock<IMatchInningsUrlParser>();
            urlParser.Setup(x => x.ParseInningsOrderInMatchFromUrl(It.IsAny<Uri>())).Returns<int?>(null);

            using (var controller = new TestController(matchDataSource.Object, new Uri("https://example.org/matches/example-match"), urlParser.Object, UmbracoHelper))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<HttpNotFoundResult>(result);
            }
        }

        [Theory]
        [InlineData(MatchResultType.HomeWin)]
        [InlineData(MatchResultType.AwayWin)]
        [InlineData(MatchResultType.Tie)]
        [InlineData(MatchResultType.AbandonedDuringPlayAndPostponed)]
        [InlineData(MatchResultType.AbandonedDuringPlayAndCancelled)]
        [InlineData(null)]
        public async Task Route_matching_match_played_in_the_past_returns_EditScorecardViewModel(MatchResultType? matchResultType)
        {
            var matchDataSource = new Mock<IMatchDataSource>();
            matchDataSource.Setup(x => x.ReadMatchByRoute(It.IsAny<string>())).ReturnsAsync(new Stoolball.Matches.Match
            {
                StartTime = DateTime.UtcNow.AddHours(-1),
                Season = new Season(),
                MatchResultType = matchResultType,
                MatchInnings = new List<MatchInnings> {
                    new MatchInnings{ InningsOrderInMatch = 1 }
                }
            });

            var urlParser = new Mock<IMatchInningsUrlParser>();
            urlParser.Setup(x => x.ParseInningsOrderInMatchFromUrl(It.IsAny<Uri>())).Returns(1);

            using (var controller = new TestController(matchDataSource.Object, new Uri("https://example.org/matches/example-match"), urlParser.Object, UmbracoHelper))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.IsType<EditScorecardViewModel>(((ViewResult)result).Model);
            }
        }

        [Fact]
        public async Task ModelU002ECurrentInningsU002EOvers_defaults_to_12()
        {
            var matchDataSource = new Mock<IMatchDataSource>();
            matchDataSource.Setup(x => x.ReadMatchByRoute(It.IsAny<string>())).ReturnsAsync(new Stoolball.Matches.Match
            {
                StartTime = DateTime.UtcNow.AddHours(-1),
                Season = new Season(),
                MatchResultType = MatchResultType.HomeWin,
                MatchInnings = new List<MatchInnings> {
                    new MatchInnings{ InningsOrderInMatch = 1 }
                }
            });

            var urlParser = new Mock<IMatchInningsUrlParser>();
            urlParser.Setup(x => x.ParseInningsOrderInMatchFromUrl(It.IsAny<Uri>())).Returns(1);

            using (var controller = new TestController(matchDataSource.Object, new Uri("https://example.org/matches/example-match"), urlParser.Object, UmbracoHelper))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.Equal(12, ((EditScorecardViewModel)((ViewResult)result).Model).CurrentInnings.Overs);
            }
        }

        [Fact]
        public async Task ModelU002ECurrentInningsU002EOvers_defaults_to_6_for_tournaments()
        {
            var matchDataSource = new Mock<IMatchDataSource>();
            matchDataSource.Setup(x => x.ReadMatchByRoute(It.IsAny<string>())).ReturnsAsync(new Stoolball.Matches.Match
            {
                StartTime = DateTime.UtcNow.AddHours(-1),
                Season = new Season(),
                MatchResultType = MatchResultType.HomeWin,
                MatchInnings = new List<MatchInnings> {
                    new MatchInnings{ InningsOrderInMatch = 1 }
                },
                Tournament = new Tournament { TournamentName = "Example tournament" }
            });

            var urlParser = new Mock<IMatchInningsUrlParser>();
            urlParser.Setup(x => x.ParseInningsOrderInMatchFromUrl(It.IsAny<Uri>())).Returns(1);

            using (var controller = new TestController(matchDataSource.Object, new Uri("https://example.org/matches/example-match"), urlParser.Object, UmbracoHelper))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.Equal(6, ((EditScorecardViewModel)((ViewResult)result).Model).CurrentInnings.Overs);
            }
        }

        [Fact]
        public async Task ModelU002ECurrentInningsU002EOversBowled_is_padded_with_empty_overs()
        {
            var matchDataSource = new Mock<IMatchDataSource>();
            matchDataSource.Setup(x => x.ReadMatchByRoute(It.IsAny<string>())).ReturnsAsync(new Stoolball.Matches.Match
            {
                StartTime = DateTime.UtcNow.AddHours(-1),
                Season = new Season(),
                MatchResultType = MatchResultType.HomeWin,
                MatchInnings = new List<MatchInnings> {
                    new MatchInnings{ InningsOrderInMatch = 1, Overs = 9, OversBowled = new List<Over>() }
                },
            });

            var urlParser = new Mock<IMatchInningsUrlParser>();
            urlParser.Setup(x => x.ParseInningsOrderInMatchFromUrl(It.IsAny<Uri>())).Returns(1);

            using (var controller = new TestController(matchDataSource.Object, new Uri("https://example.org/matches/example-match"), urlParser.Object, UmbracoHelper))
            {
                var result = await controller.Index(new ContentModel(Mock.Of<IPublishedContent>())).ConfigureAwait(false);

                Assert.Equal(9, ((EditScorecardViewModel)((ViewResult)result).Model).CurrentInnings.OversBowled.Count);
            }
        }
    }
}
