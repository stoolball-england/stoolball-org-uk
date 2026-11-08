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
using Stoolball.Web.Navigation;
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
    public class EditTeamSurfaceControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IMemberManager> _memberManager = new();
        private readonly Mock<ITeamDataSource> _teamDataSource = new();
        private readonly Mock<ITeamRepository> _teamRepository = new();
        private readonly Mock<IAuthorizationPolicy<Team>> _authorizationPolicy = new();
        private readonly Mock<IListingCacheInvalidator<Team>> _teamCacheClearer = new();
        private readonly Mock<IListingCacheInvalidator<MatchLocation>> _matchLocationCacheClearer = new();
        private readonly Mock<ITeamBreadcrumbBuilder> _breadcrumbBuilder = new();
        private readonly MemberIdentityUser _currentMember = new() { Key = Guid.NewGuid(), Name = "Jo Bloggs" };
        private readonly Team _beforeUpdate = new() { TeamId = Guid.NewGuid(), TeamName = "Old name", TeamRoute = "/teams/old-name" };
        private readonly Team _updatedTeam = new() { TeamRoute = "/teams/updated-team" };

        public EditTeamSurfaceControllerTests()
        {
            Request.SetupGet(x => x.Form).Returns(new FormCollection(new Dictionary<string, StringValues>
            {
                { "Team.Introduction", "<p>Introduction</p>" },
                { "Team.PlayingTimes", "<p>Playing times</p>" },
                { "Team.Cost", "<p>Cost</p>" },
                { "Team.PublicContactDetails", "<p>Public contact</p>" },
                { "Team.PrivateContactDetails", "<p>Private contact</p>" }
            }));
            _teamDataSource.Setup(x => x.ReadTeamByRoute(It.IsAny<string>(), false)).ReturnsAsync(_beforeUpdate);
            _authorizationPolicy.Setup(x => x.IsAuthorized(_beforeUpdate)).ReturnsAsync(new Dictionary<AuthorizedAction, bool> { { AuthorizedAction.EditTeam, true } });
            _memberManager.Setup(x => x.GetCurrentMemberAsync()).ReturnsAsync(_currentMember);
            _teamRepository.Setup(x => x.UpdateTeam(It.IsAny<Team>(), _currentMember.Key, _currentMember.Name!)).ReturnsAsync(_updatedTeam);
        }

        private EditTeamSurfaceController CreateController()
        {
            return new EditTeamSurfaceController(
                UmbracoContextAccessor.Object,
                Mock.Of<IUmbracoDatabaseFactory>(),
                ServiceContext.CreatePartial(memberService: MemberService.Object, userService: Mock.Of<IUserService>(), localizationService: Mock.Of<ILocalizationService>()),
                AppCaches.NoCache,
                Mock.Of<IProfilingLogger>(),
                Mock.Of<IPublishedUrlProvider>(),
                _memberManager.Object,
                _teamDataSource.Object,
                _teamRepository.Object,
                _authorizationPolicy.Object,
                _teamCacheClearer.Object,
                _matchLocationCacheClearer.Object,
                _breadcrumbBuilder.Object)
            {
                ControllerContext = ControllerContext
            };
        }

        private static Team ValidTeam() => new() { TeamName = "New name", AgeRangeLower = 11 };

        private void VerifyNothingUpdated()
        {
            _teamRepository.Verify(x => x.UpdateTeam(It.IsAny<Team>(), It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
            _teamCacheClearer.Verify(x => x.InvalidateCache(), Times.Never);
            _matchLocationCacheClearer.Verify(x => x.InvalidateCache(), Times.Never);
        }

        [Fact]
        public async Task Null_team_throws_ArgumentNullException()
        {
            using var controller = CreateController();

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await controller.UpdateTeam(null!));
        }

        [Fact]
        public async Task Team_id_and_route_are_taken_from_the_team_being_edited()
        {
            var team = ValidTeam();

            using var controller = CreateController();

            await controller.UpdateTeam(team);

            Assert.Equal(_beforeUpdate.TeamId, team.TeamId);
            Assert.Equal(_beforeUpdate.TeamRoute, team.TeamRoute);
        }

        [Fact]
        public async Task Text_fields_are_read_from_form_so_that_HTML_is_allowed()
        {
            var team = ValidTeam();

            using var controller = CreateController();

            await controller.UpdateTeam(team);

            Assert.Equal("<p>Introduction</p>", team.Introduction);
            Assert.Equal("<p>Playing times</p>", team.PlayingTimes);
            Assert.Equal("<p>Cost</p>", team.Cost);
            Assert.Equal("<p>Public contact</p>", team.PublicContactDetails);
            Assert.Equal("<p>Private contact</p>", team.PrivateContactDetails);
        }

        [Fact]
        public async Task Valid_request_updates_team_clears_caches_and_redirects_to_team_actions()
        {
            var team = ValidTeam();

            using var controller = CreateController();

            var result = await controller.UpdateTeam(team);

            _teamRepository.Verify(x => x.UpdateTeam(team, _currentMember.Key, _currentMember.Name!), Times.Once);
            _teamCacheClearer.Verify(x => x.InvalidateCache(), Times.Once);
            _matchLocationCacheClearer.Verify(x => x.InvalidateCache(), Times.Once);
            var redirect = Assert.IsType<RedirectResult>(result);
            Assert.Equal("/teams/updated-team/edit", redirect.Url);
        }

        [Fact]
        public async Task Not_authorised_returns_EditTeam_view_without_updating_team()
        {
            _authorizationPolicy.Setup(x => x.IsAuthorized(_beforeUpdate)).ReturnsAsync(new Dictionary<AuthorizedAction, bool> { { AuthorizedAction.EditTeam, false } });

            using var controller = CreateController();

            var result = await controller.UpdateTeam(ValidTeam());

            var view = Assert.IsType<ViewResult>(result);
            Assert.Equal("EditTeam", view.ViewName);
            VerifyNothingUpdated();
        }

        [Fact]
        public async Task Invalid_ModelState_returns_view_model_with_team_page_title_authorization_and_breadcrumbs()
        {
            var team = ValidTeam();

            using var controller = CreateController();
            controller.ModelState.AddModelError("Team.TeamName", "Any error");

            var result = await controller.UpdateTeam(team);

            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<TeamViewModel>(view.Model);
            Assert.Equal("EditTeam", view.ViewName);
            Assert.Equal(team, model.Team);
            Assert.Equal("Edit New name", model.Metadata.PageTitle);
            Assert.True(model.Authorization.CurrentMemberIsAuthorized[AuthorizedAction.EditTeam]);
            _breadcrumbBuilder.Verify(x => x.BuildBreadcrumbsForTeam(model.Breadcrumbs, team, true), Times.Once);
            VerifyNothingUpdated();
        }

        [Theory]
        [InlineData(5)]
        [InlineData(10)]
        public async Task Age_range_below_eleven_without_maximum_age_is_invalid(int lower)
        {
            var team = new Team { TeamName = "New name", AgeRangeLower = lower, AgeRangeUpper = null };

            using var controller = CreateController();

            var result = await controller.UpdateTeam(team);

            Assert.IsType<ViewResult>(result);
            Assert.Contains(controller.ModelState["Team.AgeRangeUpper"]!.Errors, x => x.ErrorMessage == "School teams and junior teams must specify a maximum age for players");
            VerifyNothingUpdated();
        }

        [Fact]
        public async Task Maximum_age_lower_than_minimum_age_is_invalid()
        {
            var team = new Team { TeamName = "New name", AgeRangeLower = 8, AgeRangeUpper = 7 };

            using var controller = CreateController();

            var result = await controller.UpdateTeam(team);

            Assert.IsType<ViewResult>(result);
            Assert.Contains(controller.ModelState["Team.AgeRangeUpper"]!.Errors, x => x.ErrorMessage == "The maximum age for players cannot be lower than the minimum age");
            VerifyNothingUpdated();
        }

        [Fact]
        public async Task Maximum_age_equal_to_minimum_age_is_valid()
        {
            var team = new Team { TeamName = "New name", AgeRangeLower = 8, AgeRangeUpper = 8 };

            using var controller = CreateController();

            var result = await controller.UpdateTeam(team);

            Assert.IsType<RedirectResult>(result);
        }

        [Fact]
        public async Task Errors_for_match_locations_are_ignored()
        {
            var team = ValidTeam();

            using var controller = CreateController();
            controller.ModelState.AddModelError("Team.MatchLocations[0].PrimaryAddressableObjectName", "Any error");

            var result = await controller.UpdateTeam(team);

            Assert.IsType<RedirectResult>(result);
            Assert.True(controller.ModelState.IsValid);
        }
    }
}
