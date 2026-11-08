using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Stoolball.Competitions;
using Stoolball.Data.Abstractions;
using Stoolball.Security;
using Stoolball.Teams;
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
    public class EditSeasonTeamsSurfaceControllerTests : UmbracoBaseTest
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

        public EditSeasonTeamsSurfaceControllerTests()
        {
            _seasonDataSource.Setup(x => x.ReadSeasonByRoute(It.IsAny<string>(), false)).ReturnsAsync(_beforeUpdate);
            _authorizationPolicy.Setup(x => x.IsAuthorized(_beforeUpdate.Competition!)).ReturnsAsync(new Dictionary<AuthorizedAction, bool> { { AuthorizedAction.EditCompetition, true } });
            _memberManager.Setup(x => x.GetCurrentMemberAsync()).ReturnsAsync(_currentMember);
        }

        private EditSeasonTeamsSurfaceController CreateController()
        {
            return new EditSeasonTeamsSurfaceController(
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
        public async Task Null_season_is_replaced_with_empty_season_with_the_id_of_the_season_being_edited()
        {
            using var controller = CreateController();

            var result = await controller.UpdateSeason(null!);

            Assert.IsType<RedirectResult>(result);
            _seasonRepository.Verify(x => x.UpdateTeams(It.Is<Season>(s => s.SeasonId == _beforeUpdate.SeasonId && s.Teams.Count == 0), _currentMember.Key, _currentMember.Name!), Times.Once);
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
        public async Task Not_authorised_returns_view_without_updating_teams()
        {
            _authorizationPolicy.Setup(x => x.IsAuthorized(_beforeUpdate.Competition!)).ReturnsAsync(new Dictionary<AuthorizedAction, bool> { { AuthorizedAction.EditCompetition, false } });

            using var controller = CreateController();

            var result = await controller.UpdateSeason(new Season());

            var view = Assert.IsType<ViewResult>(result);
            Assert.Equal("EditSeasonTeams", view.ViewName);
            _seasonRepository.Verify(x => x.UpdateTeams(It.IsAny<Season>(), It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Invalid_ModelState_returns_view_model_with_season_details_authorization_page_title_and_breadcrumbs()
        {
            var season = new Season();

            using var controller = CreateController();
            controller.ModelState.AddModelError("Any", "Any error");

            var result = await controller.UpdateSeason(season);

            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<SeasonViewModel>(view.Model);
            Assert.Equal("EditSeasonTeams", view.ViewName);
            Assert.Equal(season, model.Season);
            Assert.Equal(_beforeUpdate.Competition, model.Season!.Competition);
            Assert.Equal(_beforeUpdate.FromYear, model.Season.FromYear);
            Assert.Equal(_beforeUpdate.UntilYear, model.Season.UntilYear);
            Assert.Equal(_beforeUpdate.SeasonRoute, model.Season.SeasonRoute);
            Assert.True(model.Authorization.CurrentMemberIsAuthorized[AuthorizedAction.EditCompetition]);
            Assert.Equal($"Teams in the {_beforeUpdate.SeasonFullNameAndPlayerType()}", model.Metadata.PageTitle);
            Assert.Equal([Constants.Pages.Competitions, "Example league", _beforeUpdate.SeasonName()], model.Breadcrumbs.Select(x => x.Name).TakeLast(3));
            _seasonRepository.Verify(x => x.UpdateTeams(It.IsAny<Season>(), It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Model_state_errors_for_teams_are_ignored()
        {
            var season = new Season();
            season.Teams.Add(new TeamInSeason { Team = new Team { TeamId = Guid.NewGuid() } });

            using var controller = CreateController();
            controller.ModelState.AddModelError("Season.Teams[0].Team.TeamName", "Any error");
            controller.ModelState.AddModelError("Season.Teams[0].WithdrawnDate", "The value 'x' is not valid for Date withdrew.");

            var result = await controller.UpdateSeason(season);

            Assert.IsType<RedirectResult>(result);
            _seasonRepository.Verify(x => x.UpdateTeams(season, _currentMember.Key, _currentMember.Name!), Times.Once);
        }

        [Fact]
        public async Task Date_format_error_message_is_replaced_with_helpful_message()
        {
            using var controller = CreateController();
            controller.ModelState.AddModelError("Other", "The value 'x' is not valid for Date withdrew.");

            await controller.UpdateSeason(new Season());

            var error = Assert.Single(controller.ModelState["Other"]!.Errors);
            Assert.Equal("Enter a date in YYYY-MM-DD format.", error.ErrorMessage);
        }

        [Fact]
        public async Task Valid_request_updates_teams_and_redirects_to_season_actions()
        {
            var season = new Season();

            using var controller = CreateController();

            var result = await controller.UpdateSeason(season);

            _seasonRepository.Verify(x => x.UpdateTeams(season, _currentMember.Key, _currentMember.Name!), Times.Once);
            var redirect = Assert.IsType<RedirectResult>(result);
            Assert.Equal(_beforeUpdate.SeasonRoute + "/edit", redirect.Url);
        }
    }
}
