using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Moq;
using Stoolball.Competitions;
using Stoolball.Data.Abstractions;
using Stoolball.Matches;
using Stoolball.Security;
using Stoolball.Web.Competitions;
using Stoolball.Web.Competitions.Models;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Infrastructure.Persistence;
using Xunit;

namespace Stoolball.Web.UnitTests.Competitions
{
    public class CreateSeasonSurfaceControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IMemberManager> _memberManager = new();
        private readonly Mock<ICompetitionDataSource> _competitionDataSource = new();
        private readonly Mock<ISeasonRepository> _seasonRepository = new();
        private readonly Mock<IAuthorizationPolicy<Competition>> _authorizationPolicy = new();
        private readonly MemberIdentityUser _currentMember = new() { Key = Guid.NewGuid(), Name = "Jo Bloggs" };
        private readonly Competition _competition = new() { CompetitionName = "Example league", CompetitionRoute = "/competitions/example-league" };
        private readonly Season _createdSeason = new() { SeasonRoute = "/competitions/example-league/2026" };

        public CreateSeasonSurfaceControllerTests()
        {
            SetForm(new Dictionary<string, StringValues> { { "Season.MatchTypes", nameof(MatchType.LeagueMatch) } });
            _competitionDataSource.Setup(x => x.ReadCompetitionByRoute(It.IsAny<string>())).ReturnsAsync(_competition);
            _authorizationPolicy.Setup(x => x.IsAuthorized(_competition)).ReturnsAsync(new Dictionary<AuthorizedAction, bool> { { AuthorizedAction.EditCompetition, true } });
            _memberManager.Setup(x => x.GetCurrentMemberAsync()).ReturnsAsync(_currentMember);
            _seasonRepository.Setup(x => x.CreateSeason(It.IsAny<Season>(), _currentMember.Key, _currentMember.Name!)).ReturnsAsync(_createdSeason);
        }

        private void SetForm(Dictionary<string, StringValues> fields)
        {
            var form = new Dictionary<string, StringValues>
            {
                { "Season.Introduction", "<p>Introduction</p>" },
                { "Season.Results", "<p>Results</p>" }
            };
            foreach (var field in fields) { form[field.Key] = field.Value; }
            Request.SetupGet(x => x.Form).Returns(new FormCollection(form));
        }

        private CreateSeasonSurfaceController CreateController()
        {
            return new CreateSeasonSurfaceController(
                UmbracoContextAccessor.Object,
                Mock.Of<IUmbracoDatabaseFactory>(),
                ServiceContext,
                AppCaches.NoCache,
                Mock.Of<IProfilingLogger>(),
                Mock.Of<IPublishedUrlProvider>(),
                _memberManager.Object,
                _competitionDataSource.Object,
                _seasonRepository.Object,
                _authorizationPolicy.Object)
            {
                ControllerContext = ControllerContext
            };
        }

        [Fact]
        public async Task Null_season_throws_ArgumentNullException()
        {
            using var controller = CreateController();

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await controller.CreateSeason(null!));
        }

        [Fact]
        public async Task Introduction_and_results_are_read_from_form_so_that_HTML_is_allowed()
        {
            var season = new Season { FromYear = 2026 };

            using var controller = CreateController();

            await controller.CreateSeason(season);

            Assert.Equal("<p>Introduction</p>", season.Introduction);
            Assert.Equal("<p>Results</p>", season.Results);
        }

        [Fact]
        public async Task UntilYear_is_added_to_FromYear()
        {
            var season = new Season { FromYear = 2026, UntilYear = 1 };

            using var controller = CreateController();

            await controller.CreateSeason(season);

            Assert.Equal(2027, season.UntilYear);
        }

        [Fact]
        public async Task Over_sets_without_overs_are_removed()
        {
            var season = new Season { FromYear = 2026 };
            season.DefaultOverSets.Add(new OverSet { Overs = 10 });
            season.DefaultOverSets.Add(new OverSet());

            using var controller = CreateController();

            await controller.CreateSeason(season);

            Assert.Single(season.DefaultOverSets);
            Assert.Equal(10, season.DefaultOverSets[0].Overs);
        }

        [Fact]
        public async Task Match_types_are_parsed_from_form()
        {
            SetForm(new Dictionary<string, StringValues> { { "Season.MatchTypes", $"{MatchType.LeagueMatch},{MatchType.KnockoutMatch}" } });
            var season = new Season { FromYear = 2026 };

            using var controller = CreateController();

            await controller.CreateSeason(season);

            Assert.Equal([MatchType.LeagueMatch, MatchType.KnockoutMatch], season.MatchTypes);
        }

        [Fact]
        public async Task No_match_types_and_tournaments_disabled_returns_view_with_error()
        {
            SetForm(new Dictionary<string, StringValues> { { "Season.MatchTypes", string.Empty } });

            using var controller = CreateController();

            var result = await controller.CreateSeason(new Season { FromYear = 2026 });

            var view = Assert.IsType<ViewResult>(result);
            Assert.Equal("CreateSeason", view.ViewName);
            Assert.True(controller.ModelState.ContainsKey("Season.MatchTypes"));
            _seasonRepository.Verify(x => x.CreateSeason(It.IsAny<Season>(), It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task No_match_types_but_tournaments_enabled_creates_season()
        {
            SetForm(new Dictionary<string, StringValues> { { "Season.MatchTypes", string.Empty } });
            var season = new Season { FromYear = 2026, EnableTournaments = true };

            using var controller = CreateController();

            var result = await controller.CreateSeason(season);

            Assert.IsType<RedirectResult>(result);
            _seasonRepository.Verify(x => x.CreateSeason(season, _currentMember.Key, _currentMember.Name!), Times.Once);
        }

        [Fact]
        public async Task Season_with_same_years_as_existing_season_returns_view_with_error()
        {
            _competition.Seasons.Add(new Season { FromYear = 2026, UntilYear = 2027 });

            using var controller = CreateController();

            var result = await controller.CreateSeason(new Season { FromYear = 2026, UntilYear = 1 });

            var view = Assert.IsType<ViewResult>(result);
            Assert.Equal("CreateSeason", view.ViewName);
            Assert.True(controller.ModelState.ContainsKey("Season.FromYear"));
            _seasonRepository.Verify(x => x.CreateSeason(It.IsAny<Season>(), It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Not_authorised_returns_view_without_creating_season()
        {
            _authorizationPolicy.Setup(x => x.IsAuthorized(_competition)).ReturnsAsync(new Dictionary<AuthorizedAction, bool> { { AuthorizedAction.EditCompetition, false } });

            using var controller = CreateController();

            var result = await controller.CreateSeason(new Season { FromYear = 2026 });

            var view = Assert.IsType<ViewResult>(result);
            Assert.Equal("CreateSeason", view.ViewName);
            _seasonRepository.Verify(x => x.CreateSeason(It.IsAny<Season>(), It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Invalid_ModelState_returns_view_model_with_authorization_page_title_and_breadcrumbs()
        {
            var season = new Season { FromYear = 2026 };

            using var controller = CreateController();
            controller.ModelState.AddModelError("Season.FromYear", "Any error");

            var result = await controller.CreateSeason(season);

            var model = Assert.IsType<SeasonViewModel>(((ViewResult)result).Model);
            Assert.Equal(season, model.Season);
            Assert.Equal(_competition, model.Season!.Competition);
            Assert.True(model.Authorization.CurrentMemberIsAuthorized[AuthorizedAction.EditCompetition]);
            Assert.Equal("Add a season in the Example league", model.Metadata.PageTitle);
            Assert.Equal(Constants.Pages.Competitions, model.Breadcrumbs[^2].Name);
            Assert.Equal("Example league", model.Breadcrumbs[^1].Name);
            Assert.Equal(_competition.CompetitionRoute, model.Breadcrumbs[^1].Url?.ToString());
            _seasonRepository.Verify(x => x.CreateSeason(It.IsAny<Season>(), It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Page_title_does_not_repeat_the_when_competition_name_starts_with_it()
        {
            _competition.CompetitionName = "The Example league";

            using var controller = CreateController();
            controller.ModelState.AddModelError("Season.FromYear", "Any error");

            var result = await controller.CreateSeason(new Season { FromYear = 2026 });

            var model = Assert.IsType<SeasonViewModel>(((ViewResult)result).Model);
            Assert.Equal("Add a season in The Example league", model.Metadata.PageTitle);
        }

        [Fact]
        public async Task Invalid_ModelState_adds_empty_over_set_when_there_are_none()
        {
            using var controller = CreateController();
            controller.ModelState.AddModelError("Season.FromYear", "Any error");

            var result = await controller.CreateSeason(new Season { FromYear = 2026 });

            var model = Assert.IsType<SeasonViewModel>(((ViewResult)result).Model);
            Assert.Single(model.Season!.DefaultOverSets);
        }

        [Fact]
        public async Task Valid_request_creates_season_and_redirects_to_it()
        {
            var season = new Season { FromYear = 2026 };

            using var controller = CreateController();

            var result = await controller.CreateSeason(season);

            _seasonRepository.Verify(x => x.CreateSeason(season, _currentMember.Key, _currentMember.Name!), Times.Once);
            var redirect = Assert.IsType<RedirectResult>(result);
            Assert.Equal(_createdSeason.SeasonRoute, redirect.Url);
        }

        [Theory]
        [InlineData("NotAMatchType")]
        [InlineData("999")]
        [InlineData("LeagueMatch,NotAMatchType")]
        public async Task Invalid_match_type_returns_400_without_saving(string matchTypes)
        {
            SetForm(new Dictionary<string, StringValues> { { "Season.MatchTypes", matchTypes } });

            using var controller = CreateController();

            var result = await controller.CreateSeason(new Season { FromYear = 2026 });

            var status = Assert.IsType<StatusCodeResult>(result);
            Assert.Equal(400, status.StatusCode);
            _seasonRepository.Verify(x => x.CreateSeason(It.IsAny<Season>(), It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
        }
    }
}
