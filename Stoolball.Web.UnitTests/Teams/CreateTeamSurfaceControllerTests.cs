using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Moq;
using Stoolball.Data.Abstractions;
using Stoolball.MatchLocations;
using Stoolball.Security;
using Stoolball.Teams;
using Stoolball.Web.Teams;
using Stoolball.Web.Teams.Models;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Logging;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Security;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Infrastructure.Persistence;
using Xunit;

namespace Stoolball.Web.UnitTests.Teams
{
    public class CreateTeamSurfaceControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IMemberManager> _memberManager = new();
        private readonly Mock<ITeamRepository> _teamRepository = new();
        private readonly Mock<IAuthorizationPolicy<Team>> _authorizationPolicy = new();
        private readonly Mock<IListingCacheInvalidator<Team>> _teamCacheClearer = new();
        private readonly Mock<IListingCacheInvalidator<MatchLocation>> _matchLocationCacheClearer = new();
        private readonly MemberIdentityUser _currentMember = new() { Key = Guid.NewGuid(), Name = "Jo Bloggs", UserName = "jo@example.org" };
        private readonly Team _createdTeam = new() { TeamRoute = "/teams/example-team" };

        public CreateTeamSurfaceControllerTests()
        {
            Request.SetupGet(x => x.Form).Returns(new FormCollection(new Dictionary<string, StringValues>
            {
                { "Team.Introduction", "<p>Introduction</p>" },
                { "Team.PlayingTimes", "<p>Playing times</p>" },
                { "Team.Cost", "<p>Cost</p>" },
                { "Team.PublicContactDetails", "<p>Public contact</p>" },
                { "Team.PrivateContactDetails", "<p>Private contact</p>" }
            }));
            _authorizationPolicy.Setup(x => x.IsAuthorized(It.IsAny<Team>())).ReturnsAsync(new Dictionary<AuthorizedAction, bool> { { AuthorizedAction.CreateTeam, true } });
            _memberManager.Setup(x => x.GetCurrentMemberAsync()).ReturnsAsync(_currentMember);
            _teamRepository.Setup(x => x.CreateTeam(It.IsAny<Team>(), _currentMember.Key, _currentMember.UserName!, _currentMember.Name!)).ReturnsAsync(_createdTeam);
        }

        private CreateTeamSurfaceController CreateController()
        {
            return new CreateTeamSurfaceController(
                UmbracoContextAccessor.Object,
                Mock.Of<IUmbracoDatabaseFactory>(),
                ServiceContext.CreatePartial(memberService: MemberService.Object, userService: Mock.Of<IUserService>(), localizationService: Mock.Of<ILocalizationService>()),
                AppCaches.NoCache,
                Mock.Of<IProfilingLogger>(),
                Mock.Of<IPublishedUrlProvider>(),
                _memberManager.Object,
                _teamRepository.Object,
                _authorizationPolicy.Object,
                _teamCacheClearer.Object,
                _matchLocationCacheClearer.Object)
            {
                ControllerContext = ControllerContext
            };
        }

        private static Team ValidTeam() => new() { TeamName = "Example team", AgeRangeLower = 11 };

        private void VerifyNothingCreated()
        {
            _teamRepository.Verify(x => x.CreateTeam(It.IsAny<Team>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            _teamCacheClearer.Verify(x => x.InvalidateCache(), Times.Never);
            _matchLocationCacheClearer.Verify(x => x.InvalidateCache(), Times.Never);
        }

        [Fact]
        public async Task Null_team_throws_ArgumentNullException()
        {
            using var controller = CreateController();

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await controller.CreateTeam(null!));
        }

        [Fact]
        public async Task Text_fields_are_read_from_form_so_that_HTML_is_allowed()
        {
            var team = ValidTeam();

            using var controller = CreateController();

            await controller.CreateTeam(team);

            Assert.Equal("<p>Introduction</p>", team.Introduction);
            Assert.Equal("<p>Playing times</p>", team.PlayingTimes);
            Assert.Equal("<p>Cost</p>", team.Cost);
            Assert.Equal("<p>Public contact</p>", team.PublicContactDetails);
            Assert.Equal("<p>Private contact</p>", team.PrivateContactDetails);
        }

        [Fact]
        public async Task Valid_request_creates_team_clears_caches_and_redirects_to_it()
        {
            var team = ValidTeam();

            using var controller = CreateController();

            var result = await controller.CreateTeam(team);

            _teamRepository.Verify(x => x.CreateTeam(team, _currentMember.Key, _currentMember.UserName!, _currentMember.Name!), Times.Once);
            _teamCacheClearer.Verify(x => x.InvalidateCache(), Times.Once);
            _matchLocationCacheClearer.Verify(x => x.InvalidateCache(), Times.Once);
            var redirect = Assert.IsType<RedirectResult>(result);
            Assert.Equal(_createdTeam.TeamRoute, redirect.Url);
        }

        [Fact]
        public async Task Not_authorised_returns_CreateTeam_view_without_creating_team()
        {
            _authorizationPolicy.Setup(x => x.IsAuthorized(It.IsAny<Team>())).ReturnsAsync(new Dictionary<AuthorizedAction, bool> { { AuthorizedAction.CreateTeam, false } });

            using var controller = CreateController();

            var result = await controller.CreateTeam(ValidTeam());

            var view = Assert.IsType<ViewResult>(result);
            Assert.Equal("CreateTeam", view.ViewName);
            VerifyNothingCreated();
        }

        [Fact]
        public async Task Invalid_ModelState_returns_view_model_with_team_page_title_authorization_and_breadcrumb()
        {
            var team = ValidTeam();

            using var controller = CreateController();
            controller.ModelState.AddModelError("Team.TeamName", "Any error");

            var result = await controller.CreateTeam(team);

            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<TeamViewModel>(view.Model);
            Assert.Equal("CreateTeam", view.ViewName);
            Assert.Equal(team, model.Team);
            Assert.Equal("Add a team", model.Metadata.PageTitle);
            Assert.True(model.Authorization.CurrentMemberIsAuthorized[AuthorizedAction.CreateTeam]);
            Assert.Equal(Constants.Pages.Teams, model.Breadcrumbs[^1].Name);
            VerifyNothingCreated();
        }

        [Theory]
        [InlineData(5)]
        [InlineData(10)]
        public async Task Age_range_below_eleven_without_maximum_age_is_invalid(int lower)
        {
            var team = new Team { TeamName = "Example team", AgeRangeLower = lower, AgeRangeUpper = null };

            using var controller = CreateController();

            var result = await controller.CreateTeam(team);

            Assert.IsType<ViewResult>(result);
            Assert.Contains(controller.ModelState["Team.AgeRangeUpper"]!.Errors, x => x.ErrorMessage == "School teams and junior teams must specify a maximum age for players");
            VerifyNothingCreated();
        }

        [Fact]
        public async Task Age_range_of_eleven_or_more_without_maximum_age_is_valid()
        {
            var team = new Team { TeamName = "Example team", AgeRangeLower = 11, AgeRangeUpper = null };

            using var controller = CreateController();

            var result = await controller.CreateTeam(team);

            Assert.IsType<RedirectResult>(result);
            Assert.False(controller.ModelState.ContainsKey("Team.AgeRangeUpper"));
        }

        [Fact]
        public async Task Maximum_age_lower_than_minimum_age_is_invalid()
        {
            var team = new Team { TeamName = "Example team", AgeRangeLower = 8, AgeRangeUpper = 7 };

            using var controller = CreateController();

            var result = await controller.CreateTeam(team);

            Assert.IsType<ViewResult>(result);
            Assert.Contains(controller.ModelState["Team.AgeRangeUpper"]!.Errors, x => x.ErrorMessage == "The maximum age for players cannot be lower than the minimum age");
            VerifyNothingCreated();
        }

        [Fact]
        public async Task Maximum_age_equal_to_minimum_age_is_valid()
        {
            var team = new Team { TeamName = "Example team", AgeRangeLower = 8, AgeRangeUpper = 8 };

            using var controller = CreateController();

            var result = await controller.CreateTeam(team);

            Assert.IsType<RedirectResult>(result);
        }

        [Fact]
        public async Task Errors_for_match_locations_are_ignored()
        {
            var team = ValidTeam();

            using var controller = CreateController();
            controller.ModelState.AddModelError("Team.MatchLocations[0].PrimaryAddressableObjectName", "Any error");
            controller.ModelState.AddModelError("team.matchlocations", "Any error");

            var result = await controller.CreateTeam(team);

            Assert.IsType<RedirectResult>(result);
            Assert.True(controller.ModelState.IsValid);
        }
    }
}
