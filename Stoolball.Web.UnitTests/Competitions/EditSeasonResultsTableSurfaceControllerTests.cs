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
using Stoolball.Security;
using Stoolball.Web.Competitions;
using Stoolball.Web.Competitions.Models;
using Stoolball.Web.Routing;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Infrastructure.Persistence;
using Xunit;

namespace Stoolball.Web.UnitTests.Competitions
{
    public class EditSeasonResultsTableSurfaceControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IMemberManager> _memberManager = new();
        private readonly Mock<ISeasonDataSource> _seasonDataSource = new();
        private readonly Mock<ISeasonRepository> _seasonRepository = new();
        private readonly Mock<IAuthorizationPolicy<Competition>> _authorizationPolicy = new();
        private readonly Mock<IPostSaveRedirector> _postSaveRedirector = new();
        private readonly MemberIdentityUser _currentMember = new() { Key = Guid.NewGuid(), Name = "Jo Bloggs" };
        private readonly RedirectResult _redirect = new("/redirect-destination");
        private readonly Season _beforeUpdate = new()
        {
            SeasonId = Guid.NewGuid(),
            FromYear = 2026,
            UntilYear = 2026,
            SeasonRoute = "/competitions/example-league/2026",
            Competition = new Competition { CompetitionName = "Example league", CompetitionRoute = "/competitions/example-league" }
        };

        public EditSeasonResultsTableSurfaceControllerTests()
        {
            SetForm(new Dictionary<string, StringValues>());
            _seasonDataSource.Setup(x => x.ReadSeasonByRoute(It.IsAny<string>(), false)).ReturnsAsync(_beforeUpdate);
            _authorizationPolicy.Setup(x => x.IsAuthorized(_beforeUpdate.Competition!)).ReturnsAsync(new Dictionary<AuthorizedAction, bool> { { AuthorizedAction.EditCompetition, true } });
            _memberManager.Setup(x => x.GetCurrentMemberAsync()).ReturnsAsync(_currentMember);
            _postSaveRedirector.Setup(x => x.WorkOutRedirect(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>())).Returns(_redirect);
        }

        private void SetForm(Dictionary<string, StringValues> fields)
        {
            Request.SetupGet(x => x.Form).Returns(new FormCollection(fields));
        }

        private EditSeasonResultsTableSurfaceController CreateController()
        {
            return new EditSeasonResultsTableSurfaceController(
                UmbracoContextAccessor.Object,
                Mock.Of<IUmbracoDatabaseFactory>(),
                ServiceContext,
                AppCaches.NoCache,
                Mock.Of<IProfilingLogger>(),
                Mock.Of<IPublishedUrlProvider>(),
                _memberManager.Object,
                _seasonDataSource.Object,
                _seasonRepository.Object,
                _authorizationPolicy.Object,
                _postSaveRedirector.Object)
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
        public async Task Not_authorised_returns_view_without_updating_results_table()
        {
            _authorizationPolicy.Setup(x => x.IsAuthorized(_beforeUpdate.Competition!)).ReturnsAsync(new Dictionary<AuthorizedAction, bool> { { AuthorizedAction.EditCompetition, false } });

            using var controller = CreateController();

            var result = await controller.UpdateSeason(new Season());

            var view = Assert.IsType<ViewResult>(result);
            Assert.Equal("EditSeasonResultsTable", view.ViewName);
            _seasonRepository.Verify(x => x.UpdateResultsTable(It.IsAny<Season>(), It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Invalid_ModelState_returns_view_model_with_season_details_authorization_page_title_and_breadcrumbs()
        {
            var season = new Season();

            using var controller = CreateController();
            controller.ModelState.AddModelError("Season.PointsRules", "Any error");

            var result = await controller.UpdateSeason(season);

            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<SeasonViewModel>(view.Model);
            Assert.Equal("EditSeasonResultsTable", view.ViewName);
            Assert.Equal(season, model.Season);
            Assert.Equal(_beforeUpdate.Competition, model.Season!.Competition);
            Assert.Equal(_beforeUpdate.FromYear, model.Season.FromYear);
            Assert.Equal(_beforeUpdate.UntilYear, model.Season.UntilYear);
            Assert.Equal(_beforeUpdate.SeasonRoute, model.Season.SeasonRoute);
            Assert.True(model.Authorization.CurrentMemberIsAuthorized[AuthorizedAction.EditCompetition]);
            Assert.Equal($"Edit results table for {_beforeUpdate.SeasonFullNameAndPlayerType()}", model.Metadata.PageTitle);
            Assert.Equal([Constants.Pages.Competitions, "Example league", _beforeUpdate.SeasonName()], model.Breadcrumbs.Select(x => x.Name).TakeLast(3));
            _seasonRepository.Verify(x => x.UpdateResultsTable(It.IsAny<Season>(), It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Invalid_ModelState_returns_UrlReferrer_from_form()
        {
            SetForm(new Dictionary<string, StringValues> { { "UrlReferrer", "https://www.stoolball.org.uk/competitions/example-league/2026/statistics" } });

            using var controller = CreateController();
            controller.ModelState.AddModelError("Season.PointsRules", "Any error");

            var result = await controller.UpdateSeason(new Season());

            var model = Assert.IsType<SeasonViewModel>(((ViewResult)result).Model);
            Assert.Equal(new Uri("https://www.stoolball.org.uk/competitions/example-league/2026/statistics"), model.UrlReferrer);
        }

        [Fact]
        public async Task Invalid_ModelState_without_UrlReferrer_returns_null_UrlReferrer()
        {
            using var controller = CreateController();
            controller.ModelState.AddModelError("Season.PointsRules", "Any error");

            var result = await controller.UpdateSeason(new Season());

            var model = Assert.IsType<SeasonViewModel>(((ViewResult)result).Model);
            Assert.Null(model.UrlReferrer);
        }

        [Fact]
        public async Task Valid_request_updates_results_table_and_returns_redirect_from_PostSaveRedirector()
        {
            SetForm(new Dictionary<string, StringValues> { { "UrlReferrer", "https://www.stoolball.org.uk/competitions/example-league/2026/table" } });
            var season = new Season();

            using var controller = CreateController();

            var result = await controller.UpdateSeason(season);

            _seasonRepository.Verify(x => x.UpdateResultsTable(season, _currentMember.Key, _currentMember.Name!), Times.Once);
            _postSaveRedirector.Verify(x => x.WorkOutRedirect(_beforeUpdate.SeasonRoute!, _beforeUpdate.SeasonRoute!, "/edit", "https://www.stoolball.org.uk/competitions/example-league/2026/table", null), Times.Once);
            Assert.Equal(_redirect, result);
        }
    }
}
