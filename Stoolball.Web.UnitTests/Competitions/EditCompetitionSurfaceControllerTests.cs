using System;
using System.Collections.Generic;
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
    public class EditCompetitionSurfaceControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IMemberManager> _memberManager = new();
        private readonly Mock<ICompetitionDataSource> _competitionDataSource = new();
        private readonly Mock<ICompetitionRepository> _competitionRepository = new();
        private readonly Mock<IAuthorizationPolicy<Competition>> _authorizationPolicy = new();
        private readonly Mock<IPostSaveRedirector> _postSaveRedirector = new();
        private readonly Mock<IListingCacheInvalidator<Competition>> _cacheClearer = new();
        private readonly MemberIdentityUser _currentMember = new() { Key = Guid.NewGuid(), Name = "Jo Bloggs" };
        private readonly Competition _beforeUpdate = new() { CompetitionId = Guid.NewGuid(), CompetitionName = "Old name", CompetitionRoute = "/competitions/old-name" };
        private readonly Competition _afterUpdate = new() { CompetitionId = Guid.NewGuid(), CompetitionName = "New name", CompetitionRoute = "/competitions/new-name" };
        private readonly RedirectResult _redirect = new("/competitions/new-name/edit");
        private Dictionary<string, StringValues> _form = new();

        public EditCompetitionSurfaceControllerTests()
        {
            SetForm(new Dictionary<string, StringValues>());
            _competitionDataSource.Setup(x => x.ReadCompetitionByRoute(It.IsAny<string>())).ReturnsAsync(_beforeUpdate);
            _authorizationPolicy.Setup(x => x.IsAuthorized(_beforeUpdate)).ReturnsAsync(new Dictionary<AuthorizedAction, bool> { { AuthorizedAction.EditCompetition, true } });
            _memberManager.Setup(x => x.GetCurrentMemberAsync()).ReturnsAsync(_currentMember);
            _competitionRepository.Setup(x => x.UpdateCompetition(It.IsAny<Competition>(), _currentMember.Key, _currentMember.Name!)).ReturnsAsync(_afterUpdate);
            _postSaveRedirector.Setup(x => x.WorkOutRedirect(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>())).Returns(_redirect);
        }

        private void SetForm(Dictionary<string, StringValues> additionalFields)
        {
            _form = new Dictionary<string, StringValues>
            {
                { "Competition.Introduction", "<p>Introduction</p>" },
                { "Competition.PublicContactDetails", "<p>Public contact</p>" },
                { "Competition.PrivateContactDetails", "<p>Private contact</p>" }
            };
            foreach (var field in additionalFields) { _form[field.Key] = field.Value; }
            Request.SetupGet(x => x.Form).Returns(new FormCollection(_form));
        }

        private EditCompetitionSurfaceController CreateController()
        {
            return new EditCompetitionSurfaceController(
                UmbracoContextAccessor.Object,
                Mock.Of<IUmbracoDatabaseFactory>(),
                ServiceContext,
                AppCaches.NoCache,
                Mock.Of<IProfilingLogger>(),
                Mock.Of<IPublishedUrlProvider>(),
                _memberManager.Object,
                _competitionDataSource.Object,
                _competitionRepository.Object,
                _authorizationPolicy.Object,
                _postSaveRedirector.Object,
                _cacheClearer.Object)
            {
                ControllerContext = ControllerContext
            };
        }

        [Fact]
        public async Task Null_competition_throws_ArgumentNullException()
        {
            using var controller = CreateController();

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await controller.UpdateCompetition(null!));
        }

        [Fact]
        public async Task Id_and_route_are_copied_from_existing_competition()
        {
            var competition = new Competition { CompetitionName = "New name" };

            using var controller = CreateController();

            await controller.UpdateCompetition(competition);

            Assert.Equal(_beforeUpdate.CompetitionId, competition.CompetitionId);
            Assert.Equal(_beforeUpdate.CompetitionRoute, competition.CompetitionRoute);
        }

        [Fact]
        public async Task Introduction_and_contact_details_are_read_from_form_so_that_HTML_is_allowed()
        {
            var competition = new Competition { CompetitionName = "New name" };

            using var controller = CreateController();

            await controller.UpdateCompetition(competition);

            Assert.Equal("<p>Introduction</p>", competition.Introduction);
            Assert.Equal("<p>Public contact</p>", competition.PublicContactDetails);
            Assert.Equal("<p>Private contact</p>", competition.PrivateContactDetails);
        }

        [Fact]
        public async Task Not_authorised_returns_EditCompetition_view_without_updating_competition()
        {
            _authorizationPolicy.Setup(x => x.IsAuthorized(_beforeUpdate)).ReturnsAsync(new Dictionary<AuthorizedAction, bool> { { AuthorizedAction.EditCompetition, false } });

            using var controller = CreateController();

            var result = await controller.UpdateCompetition(new Competition { CompetitionName = "New name" });

            Assert.Equal("EditCompetition", Assert.IsType<ViewResult>(result).ViewName);
            _competitionRepository.Verify(x => x.UpdateCompetition(It.IsAny<Competition>(), It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
            _cacheClearer.Verify(x => x.InvalidateCache(), Times.Never);
        }

        [Fact]
        public async Task Invalid_ModelState_returns_view_model_with_page_title_authorization_and_breadcrumbs()
        {
            var competition = new Competition { CompetitionName = "New name" };

            using var controller = CreateController();
            controller.ModelState.AddModelError("Competition.CompetitionName", "Any error");

            var result = await controller.UpdateCompetition(competition);

            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<CompetitionViewModel>(view.Model);
            Assert.Equal("EditCompetition", view.ViewName);
            Assert.Equal(competition, model.Competition);
            Assert.Equal("Edit New name", model.Metadata.PageTitle);
            Assert.True(model.Authorization.CurrentMemberIsAuthorized[AuthorizedAction.EditCompetition]);
            Assert.Equal(Constants.Pages.Competitions, model.Breadcrumbs[^2].Name);
            Assert.Equal("New name", model.Breadcrumbs[^1].Name);
            Assert.Equal(_beforeUpdate.CompetitionRoute, model.Breadcrumbs[^1].Url!.ToString());
            _competitionRepository.Verify(x => x.UpdateCompetition(It.IsAny<Competition>(), It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
            _cacheClearer.Verify(x => x.InvalidateCache(), Times.Never);
        }

        [Fact]
        public async Task Invalid_ModelState_returns_referrer_from_form()
        {
            SetForm(new Dictionary<string, StringValues> { { "UrlReferrer", "https://www.stoolball.org.uk/competitions/old-name" } });

            using var controller = CreateController();
            controller.ModelState.AddModelError("Competition.CompetitionName", "Any error");

            var result = await controller.UpdateCompetition(new Competition { CompetitionName = "New name" });

            var model = Assert.IsType<CompetitionViewModel>(((ViewResult)result).Model);
            Assert.Equal(new Uri("https://www.stoolball.org.uk/competitions/old-name"), model.UrlReferrer);
        }

        [Fact]
        public async Task Invalid_ModelState_without_referrer_in_form_returns_null_referrer()
        {
            using var controller = CreateController();
            controller.ModelState.AddModelError("Competition.CompetitionName", "Any error");

            var result = await controller.UpdateCompetition(new Competition { CompetitionName = "New name" });

            var model = Assert.IsType<CompetitionViewModel>(((ViewResult)result).Model);
            Assert.Null(model.UrlReferrer);
        }

        [Fact]
        public async Task Valid_request_updates_competition_and_clears_cache()
        {
            var competition = new Competition { CompetitionName = "New name" };

            using var controller = CreateController();

            await controller.UpdateCompetition(competition);

            _competitionRepository.Verify(x => x.UpdateCompetition(competition, _currentMember.Key, _currentMember.Name!), Times.Once);
            _cacheClearer.Verify(x => x.InvalidateCache(), Times.Once);
        }

        [Fact]
        public async Task Valid_request_redirects_using_post_save_redirector()
        {
            SetForm(new Dictionary<string, StringValues> { { "UrlReferrer", "https://www.stoolball.org.uk/competitions/old-name" } });

            using var controller = CreateController();

            var result = await controller.UpdateCompetition(new Competition { CompetitionName = "New name" });

            Assert.Same(_redirect, result);
            _postSaveRedirector.Verify(x => x.WorkOutRedirect(_beforeUpdate.CompetitionRoute!, _afterUpdate.CompetitionRoute!, "/edit", "https://www.stoolball.org.uk/competitions/old-name", null), Times.Once);
        }
    }
}
