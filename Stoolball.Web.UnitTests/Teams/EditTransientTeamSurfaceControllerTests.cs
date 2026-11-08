using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Moq;
using Stoolball.Data.Abstractions;
using Stoolball.Dates;
using Stoolball.Matches;
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
    public class EditTransientTeamSurfaceControllerTests : UmbracoBaseTest
    {
        private readonly Mock<IMemberManager> _memberManager = new();
        private readonly Mock<ITeamDataSource> _teamDataSource = new();
        private readonly Mock<ITeamRepository> _teamRepository = new();
        private readonly Mock<IMatchListingDataSource> _matchDataSource = new();
        private readonly Mock<IAuthorizationPolicy<Team>> _authorizationPolicy = new();
        private readonly Mock<IDateTimeFormatter> _dateFormatter = new();
        private readonly MemberIdentityUser _currentMember = new() { Key = Guid.NewGuid(), Name = "Jo Bloggs" };
        private readonly Team _beforeUpdate = new() { TeamId = Guid.NewGuid(), TeamName = "Old name", TeamRoute = "/teams/old-name" };
        private readonly Team _updatedTeam = new() { TeamRoute = "/teams/updated-team" };
        private readonly MatchListing _match = new()
        {
            MatchName = "Example match",
            MatchRoute = "/matches/example-match",
            StartTime = new DateTimeOffset(2026, 5, 1, 14, 0, 0, TimeSpan.Zero)
        };

        public EditTransientTeamSurfaceControllerTests()
        {
            Request.SetupGet(x => x.Form).Returns(new FormCollection(new Dictionary<string, StringValues>
            {
                { "Team.Introduction", "<p>Introduction</p>" },
                { "Team.Cost", "<p>Cost</p>" },
                { "Team.PublicContactDetails", "<p>Public contact</p>" },
                { "Team.PrivateContactDetails", "<p>Private contact</p>" }
            }));
            _teamDataSource.Setup(x => x.ReadTeamByRoute(It.IsAny<string>(), false)).ReturnsAsync(_beforeUpdate);
            _authorizationPolicy.Setup(x => x.IsAuthorized(_beforeUpdate)).ReturnsAsync(new Dictionary<AuthorizedAction, bool> { { AuthorizedAction.EditTeam, true } });
            _memberManager.Setup(x => x.GetCurrentMemberAsync()).ReturnsAsync(_currentMember);
            _teamRepository.Setup(x => x.UpdateTransientTeam(It.IsAny<Team>(), _currentMember.Key, _currentMember.Name!)).ReturnsAsync(_updatedTeam);
            _matchDataSource.Setup(x => x.ReadMatchListings(It.IsAny<MatchFilter>(), MatchSortOrder.MatchDateEarliestFirst)).ReturnsAsync(new List<MatchListing> { _match });
            _dateFormatter.Setup(x => x.FormatDate(_match.StartTime, false, false, false)).Returns("1 May 2026");
        }

        private EditTransientTeamSurfaceController CreateController()
        {
            return new EditTransientTeamSurfaceController(
                UmbracoContextAccessor.Object,
                Mock.Of<IUmbracoDatabaseFactory>(),
                ServiceContext.CreatePartial(memberService: MemberService.Object, userService: Mock.Of<IUserService>(), localizationService: Mock.Of<ILocalizationService>()),
                AppCaches.NoCache,
                Mock.Of<IProfilingLogger>(),
                Mock.Of<IPublishedUrlProvider>(),
                _memberManager.Object,
                _teamDataSource.Object,
                _teamRepository.Object,
                _matchDataSource.Object,
                _authorizationPolicy.Object,
                _dateFormatter.Object)
            {
                ControllerContext = ControllerContext
            };
        }

        private static Team ValidTeam() => new() { TeamName = "New name", AgeRangeLower = 11 };

        private void VerifyNothingUpdated()
        {
            _teamRepository.Verify(x => x.UpdateTransientTeam(It.IsAny<Team>(), It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Null_team_throws_ArgumentNullException()
        {
            using var controller = CreateController();

            await Assert.ThrowsAsync<ArgumentNullException>(async () => await controller.UpdateTransientTeam(null!));
        }

        [Fact]
        public async Task Team_id_and_route_are_taken_from_the_team_being_edited()
        {
            var team = ValidTeam();

            using var controller = CreateController();

            await controller.UpdateTransientTeam(team);

            Assert.Equal(_beforeUpdate.TeamId, team.TeamId);
            Assert.Equal(_beforeUpdate.TeamRoute, team.TeamRoute);
        }

        [Fact]
        public async Task Text_fields_are_read_from_form_so_that_HTML_is_allowed()
        {
            var team = ValidTeam();

            using var controller = CreateController();

            await controller.UpdateTransientTeam(team);

            Assert.Equal("<p>Introduction</p>", team.Introduction);
            Assert.Equal("<p>Cost</p>", team.Cost);
            Assert.Equal("<p>Public contact</p>", team.PublicContactDetails);
            Assert.Equal("<p>Private contact</p>", team.PrivateContactDetails);
        }

        [Fact]
        public async Task Valid_request_updates_team_and_redirects_to_the_team()
        {
            var team = ValidTeam();

            using var controller = CreateController();

            var result = await controller.UpdateTransientTeam(team);

            _teamRepository.Verify(x => x.UpdateTransientTeam(team, _currentMember.Key, _currentMember.Name!), Times.Once);
            var redirect = Assert.IsType<RedirectResult>(result);
            Assert.Equal("/teams/updated-team", redirect.Url);
        }

        [Fact]
        public async Task Not_authorised_returns_EditTransientTeam_view_without_updating_team()
        {
            _authorizationPolicy.Setup(x => x.IsAuthorized(_beforeUpdate)).ReturnsAsync(new Dictionary<AuthorizedAction, bool> { { AuthorizedAction.EditTeam, false } });

            using var controller = CreateController();

            var result = await controller.UpdateTransientTeam(ValidTeam());

            var view = Assert.IsType<ViewResult>(result);
            Assert.Equal("EditTransientTeam", view.ViewName);
            VerifyNothingUpdated();
        }

        [Fact]
        public async Task Invalid_ModelState_returns_view_model_with_team_matches_page_title_authorization_and_breadcrumb()
        {
            var team = ValidTeam();

            using var controller = CreateController();
            controller.ModelState.AddModelError("Team.TeamName", "Any error");

            var result = await controller.UpdateTransientTeam(team);

            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<TeamViewModel>(view.Model);
            Assert.Equal("EditTransientTeam", view.ViewName);
            Assert.Equal(team, model.Team);
            Assert.Equal([_match], model.Matches.Matches);
            Assert.Equal("Edit New name, 1 May 2026", model.Metadata.PageTitle);
            Assert.True(model.Authorization.CurrentMemberIsAuthorized[AuthorizedAction.EditTeam]);
            var breadcrumb = model.Breadcrumbs[^1];
            Assert.Equal("Example match", breadcrumb.Name);
            Assert.Equal("/matches/example-match", breadcrumb.Url?.ToString());
            _matchDataSource.Verify(x => x.ReadMatchListings(It.Is<MatchFilter>(f => f.TeamIds.Count == 1 && f.TeamIds[0] == _beforeUpdate.TeamId && !f.IncludeMatches), MatchSortOrder.MatchDateEarliestFirst), Times.Once);
            VerifyNothingUpdated();
        }

        [Theory]
        [InlineData(5)]
        [InlineData(10)]
        public async Task Age_range_below_eleven_without_maximum_age_is_invalid(int lower)
        {
            var team = new Team { TeamName = "New name", AgeRangeLower = lower, AgeRangeUpper = null };

            using var controller = CreateController();

            var result = await controller.UpdateTransientTeam(team);

            Assert.IsType<ViewResult>(result);
            Assert.Contains(controller.ModelState["Team.AgeRangeUpper"]!.Errors, x => x.ErrorMessage == "School teams and junior teams must specify a maximum age for players");
            VerifyNothingUpdated();
        }

        [Fact]
        public async Task Maximum_age_lower_than_minimum_age_is_invalid()
        {
            var team = new Team { TeamName = "New name", AgeRangeLower = 8, AgeRangeUpper = 7 };

            using var controller = CreateController();

            var result = await controller.UpdateTransientTeam(team);

            Assert.IsType<ViewResult>(result);
            Assert.Contains(controller.ModelState["Team.AgeRangeUpper"]!.Errors, x => x.ErrorMessage == "The maximum age for players cannot be lower than the minimum age");
            VerifyNothingUpdated();
        }

        [Fact]
        public async Task Maximum_age_equal_to_minimum_age_is_valid()
        {
            var team = new Team { TeamName = "New name", AgeRangeLower = 8, AgeRangeUpper = 8 };

            using var controller = CreateController();

            var result = await controller.UpdateTransientTeam(team);

            Assert.IsType<RedirectResult>(result);
        }
    }
}
