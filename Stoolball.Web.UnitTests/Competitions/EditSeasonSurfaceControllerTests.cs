using System;
using System.Collections.Generic;
using System.Linq;
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
    public class EditSeasonSurfaceControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IMemberManager> _memberManager = new();
        private readonly Mock<ISeasonDataSource> _seasonDataSource = new();
        private readonly Mock<ISeasonRepository> _seasonRepository = new();
        private readonly Mock<IAuthorizationPolicy<Competition>> _authorizationPolicy = new();
        private readonly MemberIdentityUser _currentMember = new() { Key = Guid.NewGuid(), Name = "Jo Bloggs" };
        private readonly Season _beforeUpdate = new()
        {
            SeasonId = Guid.NewGuid(),
            FromYear = 2026,
            UntilYear = 2026,
            SeasonRoute = "/competitions/example-league/2026",
            Competition = new Competition { CompetitionName = "Example league", CompetitionRoute = "/competitions/example-league" }
        };

        public EditSeasonSurfaceControllerTests()
        {
            SetForm(new Dictionary<string, StringValues> { { "Season.MatchTypes", nameof(MatchType.LeagueMatch) } });
            _seasonDataSource.Setup(x => x.ReadSeasonByRoute(It.IsAny<string>(), false)).ReturnsAsync(_beforeUpdate);
            _authorizationPolicy.Setup(x => x.IsAuthorized(_beforeUpdate.Competition!)).ReturnsAsync(new Dictionary<AuthorizedAction, bool> { { AuthorizedAction.EditCompetition, true } });
            _memberManager.Setup(x => x.GetCurrentMemberAsync()).ReturnsAsync(_currentMember);
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

        private EditSeasonSurfaceController CreateController()
        {
            return new EditSeasonSurfaceController(
                UmbracoContextAccessor.Object,
                Mock.Of<IUmbracoDatabaseFactory>(),
                ServiceContext,
                AppCaches.NoCache,
                Mock.Of<IProfilingLogger>(),
                Mock.Of<IPublishedUrlProvider>(),
                _memberManager.Object,
                _seasonDataSource.Object,
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

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await controller.UpdateSeason(null!));
        }

        [Fact]
        public async Task SeasonId_is_taken_from_the_season_being_edited()
        {
            var season = new Season { SeasonId = Guid.NewGuid() };

            using var controller = CreateController();

            await controller.UpdateSeason(season);

            Assert.Equal(_beforeUpdate.SeasonId, season.SeasonId);
        }

        [Fact]
        public async Task Introduction_and_results_are_read_from_form_so_that_HTML_is_allowed()
        {
            var season = new Season();

            using var controller = CreateController();

            await controller.UpdateSeason(season);

            Assert.Equal("<p>Introduction</p>", season.Introduction);
            Assert.Equal("<p>Results</p>", season.Results);
        }

        [Fact]
        public async Task Over_sets_without_overs_are_removed()
        {
            var season = new Season();
            season.DefaultOverSets.Add(new OverSet { Overs = 10 });
            season.DefaultOverSets.Add(new OverSet());

            using var controller = CreateController();

            await controller.UpdateSeason(season);

            Assert.Single(season.DefaultOverSets);
            Assert.Equal(10, season.DefaultOverSets[0].Overs);
        }

        [Fact]
        public async Task Match_types_are_parsed_from_form()
        {
            SetForm(new Dictionary<string, StringValues> { { "Season.MatchTypes", $"{MatchType.FriendlyMatch},{MatchType.TrainingSession}" } });
            var season = new Season();

            using var controller = CreateController();

            await controller.UpdateSeason(season);

            Assert.Equal([MatchType.FriendlyMatch, MatchType.TrainingSession], season.MatchTypes);
        }

        [Fact]
        public async Task No_match_types_and_tournaments_disabled_returns_view_with_error()
        {
            SetForm(new Dictionary<string, StringValues> { { "Season.MatchTypes", string.Empty } });

            using var controller = CreateController();

            var result = await controller.UpdateSeason(new Season());

            var view = Assert.IsType<ViewResult>(result);
            Assert.Equal("EditSeason", view.ViewName);
            Assert.True(controller.ModelState.ContainsKey("Season.MatchTypes"));
            _seasonRepository.Verify(x => x.UpdateSeason(It.IsAny<Season>(), It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task No_match_types_but_tournaments_enabled_updates_season()
        {
            SetForm(new Dictionary<string, StringValues> { { "Season.MatchTypes", string.Empty } });
            var season = new Season { EnableTournaments = true };

            using var controller = CreateController();

            var result = await controller.UpdateSeason(season);

            Assert.IsType<RedirectResult>(result);
            _seasonRepository.Verify(x => x.UpdateSeason(season, _currentMember.Key, _currentMember.Name!), Times.Once);
        }

        [Fact]
        public async Task Not_authorised_returns_view_without_updating_season()
        {
            _authorizationPolicy.Setup(x => x.IsAuthorized(_beforeUpdate.Competition!)).ReturnsAsync(new Dictionary<AuthorizedAction, bool> { { AuthorizedAction.EditCompetition, false } });

            using var controller = CreateController();

            var result = await controller.UpdateSeason(new Season());

            var view = Assert.IsType<ViewResult>(result);
            Assert.Equal("EditSeason", view.ViewName);
            _seasonRepository.Verify(x => x.UpdateSeason(It.IsAny<Season>(), It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Invalid_ModelState_returns_view_model_with_season_authorization_page_title_and_breadcrumbs()
        {
            using var controller = CreateController();
            controller.ModelState.AddModelError("Season.PlayersPerTeam", "Any error");

            var result = await controller.UpdateSeason(new Season());

            var model = Assert.IsType<SeasonViewModel>(((ViewResult)result).Model);
            Assert.Equal(_beforeUpdate, model.Season);
            Assert.True(model.Authorization.CurrentMemberIsAuthorized[AuthorizedAction.EditCompetition]);
            Assert.Equal($"Edit {_beforeUpdate.SeasonFullNameAndPlayerType()}", model.Metadata.PageTitle);
            Assert.Equal([Constants.Pages.Competitions, "Example league", _beforeUpdate.SeasonName()], model.Breadcrumbs.Select(x => x.Name).TakeLast(3));
            _seasonRepository.Verify(x => x.UpdateSeason(It.IsAny<Season>(), It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Valid_request_updates_season_and_redirects_to_season_actions()
        {
            var season = new Season();

            using var controller = CreateController();

            var result = await controller.UpdateSeason(season);

            _seasonRepository.Verify(x => x.UpdateSeason(season, _currentMember.Key, _currentMember.Name!), Times.Once);
            var redirect = Assert.IsType<RedirectResult>(result);
            Assert.Equal(_beforeUpdate.SeasonRoute + "/edit", redirect.Url);
        }

        [Theory]
        [InlineData("NotAMatchType")]
        [InlineData("999")]
        [InlineData("LeagueMatch,NotAMatchType")]
        public async Task Invalid_match_type_returns_400_without_saving(string matchTypes)
        {
            SetForm(new Dictionary<string, StringValues> { { "Season.MatchTypes", matchTypes } });

            using var controller = CreateController();

            var result = await controller.UpdateSeason(new Season());

            var status = Assert.IsType<StatusCodeResult>(result);
            Assert.Equal(400, status.StatusCode);
            _seasonRepository.Verify(x => x.UpdateSeason(It.IsAny<Season>(), It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
        }
    }
}
