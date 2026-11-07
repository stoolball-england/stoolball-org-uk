using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Moq;
using Stoolball.Competitions;
using Stoolball.Data.Abstractions;
using Stoolball.Routing;
using Stoolball.Security;
using Stoolball.Web.Competitions;
using Stoolball.Web.Competitions.Models;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Persistence;
using Xunit;

namespace Stoolball.Web.UnitTests.Competitions
{
    public class CreateCompetitionSurfaceControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IMemberManager> _memberManager = new();
        private readonly Mock<ICompetitionRepository> _competitionRepository = new();
        private readonly Mock<IAuthorizationPolicy<Competition>> _authorizationPolicy = new();
        private readonly Mock<IRouteGenerator> _routeGenerator = new();
        private readonly Mock<IListingCacheInvalidator<Competition>> _cacheClearer = new();
        private readonly Mock<IMemberGroupService> _memberGroupService = new();
        private readonly MemberIdentityUser _currentMember = new() { Key = Guid.NewGuid(), Name = "Jo Bloggs" };
        private readonly Competition _createdCompetition = new() { CompetitionRoute = "/competitions/example-league" };

        public CreateCompetitionSurfaceControllerTests()
        {
            Request.SetupGet(x => x.Form).Returns(new FormCollection(new Dictionary<string, StringValues>
            {
                { "Competition.Introduction", "<p>Introduction</p>" },
                { "Competition.PublicContactDetails", "<p>Public contact</p>" },
                { "Competition.PrivateContactDetails", "<p>Private contact</p>" }
            }));
            _routeGenerator.Setup(x => x.GenerateRoute("competition", It.IsAny<string>(), It.IsAny<IEnumerable<string>>())).Returns("competition/example-league");
            _authorizationPolicy.Setup(x => x.IsAuthorized(It.IsAny<Competition>())).ReturnsAsync(new Dictionary<AuthorizedAction, bool> { { AuthorizedAction.CreateCompetition, true } });
            _memberManager.Setup(x => x.GetCurrentMemberAsync()).ReturnsAsync(_currentMember);
            _memberManager.Setup(x => x.IsMemberAuthorizedAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<int>>())).ReturnsAsync(true);
            _competitionRepository.Setup(x => x.CreateCompetition(It.IsAny<Competition>(), _currentMember.Key, _currentMember.Name!)).ReturnsAsync(_createdCompetition);
        }

        private CreateCompetitionSurfaceController CreateController()
        {
            return new CreateCompetitionSurfaceController(
                UmbracoContextAccessor.Object,
                Mock.Of<IUmbracoDatabaseFactory>(),
                ServiceContext.CreatePartial(memberService: MemberService.Object, memberGroupService: _memberGroupService.Object, userService: Mock.Of<IUserService>(), localizationService: Mock.Of<ILocalizationService>()),
                AppCaches.NoCache,
                Mock.Of<IProfilingLogger>(),
                Mock.Of<IPublishedUrlProvider>(),
                _memberManager.Object,
                _competitionRepository.Object,
                _authorizationPolicy.Object,
                _routeGenerator.Object,
                _cacheClearer.Object)
            {
                ControllerContext = ControllerContext
            };
        }

        [Fact]
        public async Task Null_competition_throws_ArgumentNullException()
        {
            using var controller = CreateController();

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await controller.CreateCompetition(null!));
        }

        [Fact]
        public async Task Introduction_and_contact_details_are_read_from_form_so_that_HTML_is_allowed()
        {
            var competition = new Competition { CompetitionName = "Example league" };

            using var controller = CreateController();

            await controller.CreateCompetition(competition);

            Assert.Equal("<p>Introduction</p>", competition.Introduction);
            Assert.Equal("<p>Public contact</p>", competition.PublicContactDetails);
            Assert.Equal("<p>Private contact</p>", competition.PrivateContactDetails);
        }

        [Fact]
        public async Task Not_authorised_returns_CreateCompetition_view_without_creating_competition()
        {
            _authorizationPolicy.Setup(x => x.IsAuthorized(It.IsAny<Competition>())).ReturnsAsync(new Dictionary<AuthorizedAction, bool> { { AuthorizedAction.CreateCompetition, false } });

            using var controller = CreateController();

            var result = await controller.CreateCompetition(new Competition { CompetitionName = "Example league" });

            var view = Assert.IsType<ViewResult>(result);
            Assert.Equal("CreateCompetition", view.ViewName);
            _competitionRepository.Verify(x => x.CreateCompetition(It.IsAny<Competition>(), It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
            _memberGroupService.Verify(x => x.Save(It.IsAny<IMemberGroup>()), Times.Never);
            _cacheClearer.Verify(x => x.InvalidateCache(), Times.Never);
        }

        [Fact]
        public async Task Invalid_ModelState_returns_view_model_with_page_title_authorization_and_breadcrumb()
        {
            var competition = new Competition { CompetitionName = "Example league" };

            using var controller = CreateController();
            controller.ModelState.AddModelError("Competition.CompetitionName", "Any error");

            var result = await controller.CreateCompetition(competition);

            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<CompetitionViewModel>(view.Model);
            Assert.Equal("CreateCompetition", view.ViewName);
            Assert.Equal(competition, model.Competition);
            Assert.Equal("Add a competition", model.Metadata.PageTitle);
            Assert.True(model.Authorization.CurrentMemberIsAuthorized[AuthorizedAction.CreateCompetition]);
            Assert.Equal(Constants.Pages.Competitions, model.Breadcrumbs[^1].Name);
            _competitionRepository.Verify(x => x.CreateCompetition(It.IsAny<Competition>(), It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
            _cacheClearer.Verify(x => x.InvalidateCache(), Times.Never);
        }

        [Fact]
        public async Task Valid_request_creates_member_group_and_assigns_it_to_competition()
        {
            _memberGroupService.Setup(x => x.GetByName("competition/example-league")).Returns((IMemberGroup?)null);
            var competition = new Competition { CompetitionName = "Example league" };

            using var controller = CreateController();

            await controller.CreateCompetition(competition);

            _memberGroupService.Verify(x => x.Save(It.Is<IMemberGroup>(g => g.Name == "competition/example-league")), Times.Once);
            Assert.Equal("competition/example-league", competition.MemberGroupName);
            Assert.NotNull(competition.MemberGroupKey);
        }

        [Fact]
        public async Task Existing_member_group_name_is_incremented_until_unique()
        {
            _routeGenerator.Setup(x => x.IncrementRoute("competition/example-league")).Returns("competition/example-league-1");
            _memberGroupService.Setup(x => x.GetByName("competition/example-league")).Returns(Mock.Of<IMemberGroup>());
            _memberGroupService.Setup(x => x.GetByName("competition/example-league-1")).Returns((IMemberGroup?)null);
            var competition = new Competition { CompetitionName = "Example league" };

            using var controller = CreateController();

            await controller.CreateCompetition(competition);

            _memberGroupService.Verify(x => x.Save(It.Is<IMemberGroup>(g => g.Name == "competition/example-league-1")), Times.Once);
            Assert.Equal("competition/example-league-1", competition.MemberGroupName);
        }

        [Fact]
        public async Task Valid_request_creates_competition_clears_cache_and_redirects_to_it()
        {
            var competition = new Competition { CompetitionName = "Example league" };

            using var controller = CreateController();

            var result = await controller.CreateCompetition(competition);

            _competitionRepository.Verify(x => x.CreateCompetition(competition, _currentMember.Key, _currentMember.Name!), Times.Once);
            _cacheClearer.Verify(x => x.InvalidateCache(), Times.Once);
            var redirect = Assert.IsType<RedirectResult>(result);
            Assert.Equal(_createdCompetition.CompetitionRoute, redirect.Url);
        }

        [Fact]
        public async Task Administrator_is_not_assigned_to_member_group()
        {
            using var controller = CreateController();

            await controller.CreateCompetition(new Competition { CompetitionName = "Example league" });

            MemberService.Verify(x => x.AssignRole(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Non_administrator_is_assigned_to_member_group()
        {
            _memberManager.Setup(x => x.IsMemberAuthorizedAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<string>>(), It.IsAny<IEnumerable<int>>())).ReturnsAsync(false);

            using var controller = CreateController();

            await controller.CreateCompetition(new Competition { CompetitionName = "Example league" });

            MemberService.Verify(x => x.AssignRole(It.IsAny<string>(), "competition/example-league"), Times.Once);
        }
    }
}
